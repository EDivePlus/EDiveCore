using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.SerializedTypeMigration.Editor
{
    public class SerializedTypeMigrationWindow : OdinEditorWindow
    {
        [ShowInInspector, HideLabel, DisplayAsString(false), PropertyOrder(0)]
        private string _summary = "Not scanned yet.";

        [ShowInInspector, LabelText("Apply Automatically"), PropertyOrder(1)]
        [InfoBox("Rewrites assets as soon as old names show up. A branch switch becomes a big unreviewed diff.",
            InfoMessageType.Warning, VisibleIf = nameof(AutoApply))]
        private bool AutoApply
        {
            get => MigrationWatcher.AutoApply;
            set => MigrationWatcher.AutoApply = value;
        }

        [ShowInInspector, ReadOnly, ShowIf(nameof(HasErrors)), PropertyOrder(2)]
        [ListDrawerSettings(ShowFoldout = false)]
        [InfoBox("Fix these before applying.", InfoMessageType.Error)]
        private List<string> _errors = new();

        [ShowInInspector, ReadOnly, ShowIf(nameof(HasWarnings)), PropertyOrder(3)]
        [ListDrawerSettings(ShowFoldout = false)]
        [InfoBox("Does not block.", InfoMessageType.Warning)]
        private List<string> _warnings = new();

        [ShowInInspector, ReadOnly, LabelText("Pending Migrations"), PropertyOrder(4)]
        [DictionaryDrawerSettings(KeyLabel = "Migration", ValueLabel = "Assets")]
        private Dictionary<string, List<string>> _pending = new();

        [ShowInInspector, ReadOnly, ShowIf(nameof(HasOrphans)), PropertyOrder(5)]
        [DictionaryDrawerSettings(KeyLabel = "Missing Type", ValueLabel = "Assets")]
        [InfoBox("Broken: no type, no hop. Declare the old name with [FormerlySerializedType].", InfoMessageType.Error)]
        private Dictionary<string, List<string>> _orphans = new();

        [ShowInInspector, ReadOnly, ShowIf(nameof(HasBinary)), PropertyOrder(6)]
        [ListDrawerSettings(ShowFoldout = false)]
        [InfoBox("Binary, cannot rewrite. Re-serialize as text.", InfoMessageType.Error)]
        private List<string> _binary = new();

        [ShowInInspector, ReadOnly, ShowIf(nameof(HasUnreadable)), PropertyOrder(7)]
        [ListDrawerSettings(ShowFoldout = false)]
        [InfoBox("Could not read. Locked?", InfoMessageType.Warning)]
        private List<string> _unreadable = new();

        private bool HasErrors => _errors.Count > 0;
        private bool HasWarnings => _warnings.Count > 0;
        private bool HasOrphans => _orphans.Count > 0;
        private bool HasBinary => _binary.Count > 0;
        private bool HasUnreadable => _unreadable.Count > 0;
        private bool CanApply => _pending.Count > 0 && !HasErrors;

        [MenuItem("Tools/Serialized Type Migration")]
        public static void Open() => GetWindow<SerializedTypeMigrationWindow>("Serialized Types");

        [Button(ButtonSizes.Large), HorizontalGroup("Actions"), PropertyOrder(-1)]
        private void Scan()
        {
            var map = MigrationMap.Build();
            var report = MigrationScanner.Scan(map);

            _errors = map.Errors.ToList();
            _warnings = map.Warnings.ToList();
            _pending = report.Pending.ToDictionary(entry => Describe(map, entry.Key), entry => entry.Value);
            _orphans = report.Orphans.ToDictionary(entry => entry.Key.ToString(), entry => entry.Value);
            _binary = report.BinaryWithOldTypes;
            _unreadable = report.Unreadable;

            _summary = $"{map.Hops.Count} hop(s). {report.FilesWithReferences} of {report.FilesScanned} files hold managed references. " +
                       $"{_pending.Count} to migrate in {report.PendingFileCount} file(s), {_orphans.Count} orphan(s).";
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Actions"), EnableIf(nameof(CanApply)), PropertyOrder(-1)]
        private void Apply()
        {
            if (!MigrationGuard.CanApply(out var problem))
            {
                EditorUtility.DisplayDialog("Cannot migrate", problem, "OK");
                return;
            }

            var files = _pending.Values.SelectMany(paths => paths).Distinct().Count();
            if (!EditorUtility.DisplayDialog("Migrate serialized types",
                    $"Rewrites {_pending.Count} type name(s) in {files} asset(s) on disk.\n\nCommit your work first.", "Migrate", "Cancel"))
                return;

            var changed = MigrationScanner.Apply(MigrationMap.Build());
            Debug.Log($"[SerializedTypeMigration] Rewrote {changed.Count} asset(s):\n{string.Join("\n", changed)}");
            Scan();
        }

        private static string Describe(MigrationMap map, SerializedTypeIdentity id)
        {
            var state = map.UnityLoadsWithoutMigration(id) ? "loads via [MovedFrom]" : "broken until migrated";
            return $"{id}   ->   {map.Resolve(id)}   ({state})";
        }
    }
}
