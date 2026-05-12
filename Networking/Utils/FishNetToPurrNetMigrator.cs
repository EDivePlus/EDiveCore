#if UNITY_EDITOR && FISHNET
using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNetBehaviour = FishNet.Object.NetworkBehaviour;
using FishNetTransform = FishNet.Component.Transforming.NetworkTransform;
using PurrTransform = PurrNet.NetworkTransform;

namespace EDIVE.Networking.Editor.Migration
{
    /// <summary>
    /// One-shot migration utility. Iterates every prefab under <c>Assets/</c> and every enabled scene
    /// in Build Settings; removes FishNet NetworkObject, swaps FishNet NetworkTransform for the PurrNet
    /// equivalent, and logs any remaining FishNet NetworkBehaviour-derived components for manual review.
    /// </summary>
    public static class FishNetToPurrNetMigrator
    {
        private const string MENU_ROOT = "Tools/Networking/FishNet -> PurrNet";

        [MenuItem(MENU_ROOT + "/Scan (Dry Run)")]
        public static void ScanOnly() => Run(dryRun: true);

        [MenuItem(MENU_ROOT + "/Migrate")]
        public static void Migrate()
        {
            const string message =
                "This will modify all prefabs in Assets/ and all enabled scenes in Build Settings.\n\n" +
                "  - Remove FishNet NetworkObject components\n" +
                "  - Replace FishNet NetworkTransform with PurrNet NetworkTransform\n" +
                "  - Log any other FishNet NetworkBehaviour components for manual review\n\n" +
                "Make sure your project is committed to source control first.\n\nProceed?";

            if (!EditorUtility.DisplayDialog("FishNet -> PurrNet Migration", message, "Migrate", "Cancel"))
                return;

            Run(dryRun: false);
        }

        private static void Run(bool dryRun)
        {
            var report = new MigrationReport(dryRun);
            try
            {
                AssetDatabase.StartAssetEditing();
                MigratePrefabs(report);
                MigrateScenes(report);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                if (!dryRun) AssetDatabase.SaveAssets();
            }
            report.PrintSummary();
        }

        // ─── Prefabs ──────────────────────────────────────────────────────────────

        private static void MigratePrefabs(MigrationReport report)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar(
                        $"FishNet -> PurrNet: Prefabs ({i + 1}/{guids.Length})",
                        path, (float)i / guids.Length))
                    return;

                if (!IsEditablePrefab(path)) continue;
                MigratePrefab(path, report);
            }
        }

        private static bool IsEditablePrefab(string path)
        {
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return false;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return false;
            var type = PrefabUtility.GetPrefabAssetType(asset);
            return type == PrefabAssetType.Regular || type == PrefabAssetType.Variant;
        }

        private static void MigratePrefab(string path, MigrationReport report)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(path);
                var changed = MigrateGameObjectTree(contents, path, "Prefab", report);
                if (changed && !report.DryRun)
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FN->PN] Failed to process prefab {path}: {e.Message}");
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // ─── Scenes ───────────────────────────────────────────────────────────────

        private static void MigrateScenes(MigrationReport report)
        {
            var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            if (buildScenes.Length == 0) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[FN->PN] Aborted: unsaved scene changes were not saved.");
                return;
            }

            var previousScenePath = SceneManager.GetActiveScene().path;

            for (var i = 0; i < buildScenes.Length; i++)
            {
                var path = buildScenes[i].path;
                if (string.IsNullOrEmpty(path)) continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        $"FishNet -> PurrNet: Scenes ({i + 1}/{buildScenes.Length})",
                        path, (float)i / buildScenes.Length))
                    break;

                Scene scene;
                try { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single); }
                catch (Exception e)
                {
                    Debug.LogError($"[FN->PN] Failed to open scene {path}: {e.Message}");
                    continue;
                }

                var changed = false;
                foreach (var root in scene.GetRootGameObjects())
                    changed |= MigrateGameObjectTree(root, path, "Scene", report);

                if (changed && !report.DryRun)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(previousScenePath))
            {
                try { EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single); }
                catch { /* original scene may have been deleted/renamed; ignore */ }
            }
        }

        // ─── Core ─────────────────────────────────────────────────────────────────

        private static bool MigrateGameObjectTree(GameObject root, string assetPath, string assetKind, MigrationReport report)
        {
            var changed = false;

            // Replace FishNet NetworkTransform with PurrNet NetworkTransform first so it
            // isn't picked up again by the NetworkBehaviour scan below.
            foreach (var ft in root.GetComponentsInChildren<FishNetTransform>(true))
            {
                if (ft == null) continue;
                var go = ft.gameObject;
                var componentIndex = Array.IndexOf(go.GetComponents<Component>(), ft);
                report.LogReplacedTransform(assetPath, GetHierarchyPath(go));

                if (!report.DryRun)
                {
                    UnityEngine.Object.DestroyImmediate(ft, true);
                    var purr = go.AddComponent<PurrTransform>();
                    MoveComponentToIndex(purr, Math.Max(componentIndex, 1));
                }
                changed = true;
            }

            // Remove FishNet NetworkObject (it inherits from MonoBehaviour, not NetworkBehaviour).
            foreach (var no in root.GetComponentsInChildren<NetworkObject>(true))
            {
                if (no == null) continue;
                report.LogRemovedNetworkObject(assetPath, GetHierarchyPath(no.gameObject));
                if (!report.DryRun)
                    UnityEngine.Object.DestroyImmediate(no, true);
                changed = true;
            }

            // Log remaining FishNet NetworkBehaviour subclasses for manual replacement.
            foreach (var nb in root.GetComponentsInChildren<FishNetBehaviour>(true))
            {
                if (nb == null) continue;
                if (nb is FishNetTransform) continue; // already handled
                report.LogManualReview(assetPath, assetKind, GetHierarchyPath(nb.gameObject), nb.GetType());
            }

            return changed;
        }

        private static void MoveComponentToIndex(Component component, int targetIndex)
        {
            var components = component.gameObject.GetComponents<Component>();
            var currentIndex = Array.IndexOf(components, component);
            while (currentIndex > targetIndex)
            {
                if (!ComponentUtility.MoveComponentUp(component)) break;
                currentIndex--;
            }
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var stack = new Stack<string>();
            var t = go.transform;
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        // ─── Report ───────────────────────────────────────────────────────────────

        private class MigrationReport
        {
            public bool DryRun { get; }
            private int _removedNetworkObjects;
            private int _replacedTransforms;
            private readonly List<string> _manualReviewEntries = new();
            private readonly HashSet<string> _manualReviewDeduper = new();

            public MigrationReport(bool dryRun) => DryRun = dryRun;

            public void LogRemovedNetworkObject(string asset, string path)
            {
                _removedNetworkObjects++;
                Debug.Log($"[FN->PN] Remove NetworkObject -- {asset} :: {path}");
            }

            public void LogReplacedTransform(string asset, string path)
            {
                _replacedTransforms++;
                Debug.Log($"[FN->PN] Replace NetworkTransform -- {asset} :: {path}");
            }

            public void LogManualReview(string asset, string kind, string path, Type type)
            {
                var key = $"{asset}|{path}|{type.FullName}";
                if (!_manualReviewDeduper.Add(key)) return;

                var entry = $"[{kind}] {asset} :: {path} :: {type.FullName}";
                _manualReviewEntries.Add(entry);
                Debug.LogWarning($"[FN->PN] Manual review -- {entry}");
            }

            public void PrintSummary()
            {
                var prefix = DryRun ? "[DRY RUN]" : "[MIGRATED]";
                Debug.Log(
                    $"{prefix} FishNet -> PurrNet migration done. " +
                    $"NetworkObject removals: {_removedNetworkObjects}, " +
                    $"NetworkTransform replacements: {_replacedTransforms}, " +
                    $"Manual review items: {_manualReviewEntries.Count}");

                if (_manualReviewEntries.Count > 0)
                {
                    Debug.LogWarning(
                        $"{prefix} The following components inherit from FishNet.Object.NetworkBehaviour " +
                        $"and need to be migrated manually ({_manualReviewEntries.Count}):\n  " +
                        string.Join("\n  ", _manualReviewEntries.OrderBy(s => s)));
                }
            }
        }
    }
}
#endif
