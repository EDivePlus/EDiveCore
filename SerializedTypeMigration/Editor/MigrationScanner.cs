using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace EDIVE.SerializedTypeMigration.Editor
{
    public static class MigrationScanner
    {
        // files that can hold our MonoBehaviour / ScriptableObject data
        private static readonly HashSet<string> EXTENSIONS = new(StringComparer.OrdinalIgnoreCase)
        {
            ".unity", ".prefab", ".asset",
            ".controller", // StateMachineBehaviour
            ".playable",   // Timeline tracks and clips
            ".signal",     // SignalAsset subclass
            ".preset"      // preset of our component
        };

        private static List<(string fullPath, string assetPath)> _roots;

        static MigrationScanner() => Events.registeredPackages += _ => _roots = null;

        // Assets + embedded and local packages. registry packages are read only
        private static List<(string fullPath, string assetPath)> Roots => _roots ??= FindRoots();

        private static List<(string fullPath, string assetPath)> FindRoots()
        {
            var packages = PackageInfo.GetAllRegisteredPackages()
                .Where(package => package.source is PackageSource.Embedded or PackageSource.Local)
                .Where(package => Directory.Exists(package.resolvedPath))
                .Select(package => (Normalize(package.resolvedPath), package.assetPath));

            return packages.Prepend((Normalize(Application.dataPath), "Assets")).ToList();

            static string Normalize(string path) => Path.GetFullPath(path).TrimEnd('/', '\\');
        }

        public static List<string> EnumerateCandidateFiles() =>
            Roots.SelectMany(root => Directory.EnumerateFiles(root.fullPath, "*", SearchOption.AllDirectories)
                    .Where(path => IsCandidate(path[(root.fullPath.Length + 1)..])))
                .ToList();

        // relative or asset path. hidden and '~' folders skipped like Unity does
        public static bool IsCandidate(string relativePath) =>
            EXTENSIONS.Contains(Path.GetExtension(relativePath)) &&
            relativePath.Split('/', '\\').All(segment => !segment.EndsWith("~") && !segment.StartsWith("."));

        // null if not under Assets or an embedded/local package
        public static string ToAssetPath(string fullPath)
        {
            var full = Path.GetFullPath(fullPath);
            return Roots
                .Where(root => full.StartsWith(root.fullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Select(root => $"{root.assetPath}/{full[(root.fullPath.Length + 1)..].Replace('\\', '/')}")
                .FirstOrDefault();
        }

        public static MigrationReport Scan(MigrationMap map) => Scan(map, EnumerateCandidateFiles());

        public static MigrationReport Scan(MigrationMap map, IReadOnlyList<string> files)
        {
            var report = new MigrationReport { FilesScanned = files.Count };

            foreach (var file in ReadAll(files, "Scanning managed references", report))
            {
                var display = ToAssetPath(file.Path) ?? file.Path;

                if (!file.IsYaml)
                {
                    if (MentionsAny(file.Bytes, map.Hops.Keys))
                        report.BinaryWithOldTypes.Add(display);
                    continue;
                }

                report.FilesWithReferences++;
                foreach (var id in SerializedTypeYaml.Read(file.Text).Distinct())
                {
                    if (map.Resolve(id) != id)
                        MigrationReport.Record(report.Pending, id, display);
                    else if (!map.LiveTypes.Contains(id))
                        MigrationReport.Record(report.Orphans, id, display);
                }
            }
            return report;
        }

        // Scan for the import hook: no progress bar, pending only
        public static Dictionary<SerializedTypeIdentity, List<string>> FindPending(MigrationMap map, IEnumerable<string> files)
        {
            var pending = new Dictionary<SerializedTypeIdentity, List<string>>();
            foreach (var path in files)
            {
                if (!TryRead(path, out var file) || !file.IsYaml)
                    continue;

                foreach (var id in SerializedTypeYaml.Read(file.Text).Distinct().Where(id => map.Resolve(id) != id))
                    MigrationReport.Record(pending, id, ToAssetPath(path) ?? path);
            }
            return pending;
        }

        public static List<string> Apply(MigrationMap map) => Apply(map, EnumerateCandidateFiles());

        public static List<string> Apply(MigrationMap map, IReadOnlyList<string> files)
        {
            var changed = new List<string>();
            var rewrites = Rewrite(map, files);
            if (rewrites.Count == 0)
                return changed;

            // unsaved edits would be lost on unload, save them and migrate the saved file
            if (SaveOpenAssets(rewrites.Select(rewrite => rewrite.AssetPath).ToHashSet()))
                rewrites = Rewrite(map, rewrites.Select(rewrite => rewrite.Path).ToList());
            if (rewrites.Count == 0)
                return changed;

            var reopen = ReleaseOpenAssets(rewrites.Select(rewrite => rewrite.AssetPath).ToHashSet());
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var rewrite in rewrites)
                {
                    SerializedTypeYaml.WriteFile(rewrite.Path, rewrite.Text, rewrite.HasBom);
                    changed.Add(rewrite.AssetPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();

                // also on failure, files written so far must reimport
                foreach (var path in changed.Where(path => path.StartsWith("Assets/") || path.StartsWith("Packages/")))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                reopen?.Invoke();
            }

            return changed;
        }

        // binary is never written
        private static List<(string Path, string AssetPath, string Text, bool HasBom)> Rewrite(MigrationMap map, IReadOnlyList<string> files)
        {
            var rewrites = new List<(string Path, string AssetPath, string Text, bool HasBom)>();
            foreach (var file in ReadAll(files, "Migrating serialized types", new MigrationReport()).Where(file => file.IsYaml))
            {
                var result = SerializedTypeYaml.Rewrite(file.Text, map.Resolve, out var rewritten);
                if (rewritten > 0)
                    rewrites.Add((file.Path, ToAssetPath(file.Path) ?? file.Path, result, file.HasBom));
            }
            return rewrites;
        }

        // unloading an affected scene unloads all of them, so every dirty scene is saved then
        private static bool SaveOpenAssets(HashSet<string> assetPaths)
        {
            var saved = false;
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();
            if (scenes.Any(scene => assetPaths.Contains(scene.path)))
            {
                foreach (var scene in scenes.Where(scene => scene.isDirty && !string.IsNullOrEmpty(scene.path)))
                    saved |= EditorSceneManager.SaveScene(scene);
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty && assetPaths.Contains(stage.assetPath))
            {
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
                stage.ClearDirtiness();
                saved = true;
            }
            return saved;
        }

        // unity prompts when an open scene or prefab changes on disk
        private static Action ReleaseOpenAssets(HashSet<string> assetPaths)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup().Where(scene => !string.IsNullOrEmpty(scene.path)).ToArray();
            if (setup.Length > 0 && !setup.Any(scene => scene.isActive))
                setup[0].isActive = true;
            var stagePath = PrefabStageUtility.GetCurrentPrefabStage()?.assetPath;
            var sceneAffected = setup.Any(scene => assetPaths.Contains(scene.path));
            if (!sceneAffected && (stagePath == null || !assetPaths.Contains(stagePath)))
                return null;

            if (sceneAffected)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                StageUtility.GoToMainStage();

            return () =>
            {
                if (sceneAffected)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                if (stagePath != null)
                    PrefabStageUtility.OpenPrefab(stagePath);
            };
        }

        private readonly struct AssetFile
        {
            public readonly string Path;
            public readonly byte[] Bytes;
            public readonly string Text; // null when binary
            public readonly bool HasBom;

            public AssetFile(string path, byte[] bytes)
            {
                Path = path;
                Bytes = bytes;
                var text = SerializedTypeYaml.Decode(bytes, out HasBom);
                Text = SerializedTypeYaml.IsUnityYaml(text) ? text : null;
            }

            public bool IsYaml => Text != null;
        }

        // binary files + yaml that may hold references. cancel throws OperationCanceledException
        private static IEnumerable<AssetFile> ReadAll(IReadOnlyList<string> files, string title, MigrationReport report)
        {
            try
            {
                for (var i = 0; i < files.Count; i++)
                {
                    if (i % 25 == 0 && EditorUtility.DisplayCancelableProgressBar(title, $"{i} / {files.Count}", (float) i / files.Count))
                        throw new OperationCanceledException(title);

                    if (!TryRead(files[i], out var file))
                        report.Unreadable.Add(files[i]);
                    else if (!file.IsYaml || SerializedTypeYaml.MightContainReferences(file.Text))
                        yield return file;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool TryRead(string path, out AssetFile file)
        {
            try
            {
                file = new AssetFile(path, File.ReadAllBytes(path));
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                file = default;
                return false;
            }
        }

        // binary is not parsed. guess: old class, ns and asm strings all appear in the file
        private static bool MentionsAny(byte[] bytes, IEnumerable<SerializedTypeIdentity> identities) =>
            identities.Any(id =>
                Contains(bytes, id.Open().Class.Split('/').Last()) &&
                (id.Namespace.Length == 0 || Contains(bytes, id.Namespace)) &&
                Contains(bytes, id.Assembly));

        private static bool Contains(byte[] bytes, string text) => bytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text)) >= 0;
    }

    public sealed class MigrationReport
    {
        public Dictionary<SerializedTypeIdentity, List<string>> Pending { get; } = new();
        public Dictionary<SerializedTypeIdentity, List<string>> Orphans { get; } = new(); // no type, no hop
        public List<string> BinaryWithOldTypes { get; } = new();                          // cannot rewrite
        public List<string> Unreadable { get; } = new();                                  // locked files

        public int FilesScanned { get; set; }
        public int FilesWithReferences { get; set; }

        public int PendingFileCount => Pending.Values.SelectMany(paths => paths).Distinct().Count();

        internal static void Record(Dictionary<SerializedTypeIdentity, List<string>> target, SerializedTypeIdentity id, string path)
        {
            if (!target.TryGetValue(id, out var paths))
                target[id] = paths = new List<string>();

            paths.Add(path);
        }
    }

    public static class MigrationGuard
    {
        public static bool CanApply(out string problem)
        {
            problem = FindProblem();
            return problem == null;
        }

        private static string FindProblem()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Exit play mode first.";

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
                return "Asset Serialization must be Force Text.";

            // Apply saves dirty scenes and prefabs itself, an untitled one has nowhere to go
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty && string.IsNullOrEmpty(scene.path))
                    return "Save the untitled scene first.";
            }

            return null;
        }
    }
}
