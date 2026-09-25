using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EDIVE.SerializedTypeMigration.Editor
{
    // logs problems after attributes change (recompile) and when assets with old names come in (pull, merge)
    [InitializeOnLoad]
    public static class MigrationWatcher
    {
        private const string LOG_PREFIX = "[SerializedTypeMigration] ";

        private static MigrationMap _map;

        // attributes only change on recompile, build once per domain
        internal static MigrationMap Map => _map ??= MigrationMap.Build();

        // off by default: unattended rewrites turn a branch switch into a big unreviewed diff
        public static bool AutoApply
        {
            get => EditorPrefs.GetBool(Key("AutoApply"), false);
            set => EditorPrefs.SetBool(Key("AutoApply"), value);
        }

        private static bool IsBusy =>
            EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;

        static MigrationWatcher()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += CheckDeclarations;
        }

        public static void ForgetStamp() => EditorPrefs.DeleteKey(Key("Stamp"));

        // per project and machine. not GetHashCode, CoreCLR randomizes it per process
        private static string Key(string name) => $"EDIVE.SerializedTypeMigration.{name}.{Application.dataPath}";

        private static void CheckDeclarations()
        {
            if (IsBusy)
            {
                EditorApplication.delayCall += CheckDeclarations;
                return;
            }

            var map = Map;
            var stamp = Fingerprint(map);
            if (EditorPrefs.GetString(Key("Stamp"), string.Empty) == stamp)
                return;

            foreach (var error in map.Errors)
                Debug.LogError(LOG_PREFIX + error);
            foreach (var warning in map.Warnings)
                Debug.LogWarning(LOG_PREFIX + warning);

            if (!map.IsValid || map.Hops.Count == 0)
            {
                EditorPrefs.SetString(Key("Stamp"), stamp);
                return;
            }

            MigrationReport report;
            try
            {
                report = MigrationScanner.Scan(map);
            }
            catch (OperationCanceledException)
            {
                return; // no stamp, ask again next reload
            }

            EditorPrefs.SetString(Key("Stamp"), stamp);

            // usually a type moved or deleted without an attribute
            foreach (var (id, paths) in report.Orphans)
                Debug.LogError($"{LOG_PREFIX}'{id}' has no type and no hop. Declare it with [FormerlySerializedType].\n{Lines(paths)}");

            Handle(map, report.Pending);
        }

        internal static void CheckImported(IReadOnlyList<string> assetPaths)
        {
            if (IsBusy)
            {
                EditorApplication.delayCall += () => CheckImported(assetPaths);
                return;
            }

            var map = Map;
            if (map.IsValid && map.Hops.Count > 0)
                Handle(map, MigrationScanner.FindPending(map, assetPaths.Select(FileUtil.GetPhysicalPath)));
        }

        private static void Handle(MigrationMap map, IReadOnlyDictionary<SerializedTypeIdentity, List<string>> pending)
        {
            if (pending.Count == 0)
                return;

            if (AutoApply && MigrationGuard.CanApply(out _))
            {
                var files = pending.Values.SelectMany(paths => paths).Distinct().Select(FileUtil.GetPhysicalPath).ToList();
                Debug.Log($"{LOG_PREFIX}Auto migrated:\n{Lines(MigrationScanner.Apply(map, files))}");
                return;
            }

            var broken = FilesWhere(pending, id => !map.UnityLoadsWithoutMigration(id));
            if (broken.Count > 0)
                Debug.LogError($"{LOG_PREFIX}{broken.Count} asset(s) broken until migrated. Open Tools/Serialized Type Migration.\n{Lines(broken)}");

            var loading = FilesWhere(pending, map.UnityLoadsWithoutMigration).Except(broken).ToList();
            if (loading.Count > 0)
                Debug.LogWarning($"{LOG_PREFIX}{loading.Count} asset(s) rely on [MovedFrom] at load. Migrate to make it permanent.\n{Lines(loading)}");
        }

        private static List<string> FilesWhere(IReadOnlyDictionary<SerializedTypeIdentity, List<string>> pending, Func<SerializedTypeIdentity, bool> predicate) =>
            pending.Where(entry => predicate(entry.Key)).SelectMany(entry => entry.Value).Distinct().ToList();

        private static string Lines(IEnumerable<string> lines) => string.Join("\n", lines);

        private static string Fingerprint(MigrationMap map) =>
            string.Join("|", map.Hops.Select(hop => $"{hop.Key}=>{hop.Value}")
                .Concat(map.Errors)
                .Concat(map.Warnings)
                .OrderBy(entry => entry, StringComparer.Ordinal));
    }

    internal sealed class MigrationImportWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Application.isBatchMode)
                return;

            var candidates = imported.Concat(moved).Where(MigrationScanner.IsCandidate).Distinct().ToList();
            if (candidates.Count > 0)
                EditorApplication.delayCall += () => MigrationWatcher.CheckImported(candidates);
        }
    }
}
