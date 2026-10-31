using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Localization.Editor.Sheets;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;
using UnitySheets = UnityEditor.Localization.Plugins.Google.GoogleSheets;

namespace EDIVE.Localization.Editor
{
    [GlobalConfig("Assets/_Project/Settings/Editor/")]
    public class LocalizationConfig : GlobalConfig<LocalizationConfig>
    {
        [MenuItem("Tools/Localization Config %l")]
        private static void OpenLocalizationConfig()
        {
            OdinEditorWindow.InspectObject(Instance);
        }

#if !ODIN_IS_UPDATING
        [PropertyOrder(-10)]
        [PropertySpace(0, 6)]
        [IconButton("Localization Editor", FontAwesomeEditorIconType.GlobeSolid)]
        private void OpenLocalizationEditor() => OdinLocalizationEditorWindow.OpenFromMenu();
#endif

        [EnhancedBoxGroup("Defaults", Color = "@ColorTools.Green")]
        [ShowCreateNew]
        [LabelText("Sheets Provider")]
        [InlineButton(nameof(ToggleSignIn), "$" + nameof(SignInLabel), ShowIf = "@" + nameof(_DefaultSheetsServiceProvider) + " != null")]
        [SerializeField]
        private SheetsServiceProvider _DefaultSheetsServiceProvider;

        [EnhancedBoxGroup("Defaults")]
        [LabelText("Spreadsheet ID")]
        [InlineIconButton(FontAwesomeEditorIconType.UpRightFromSquareSolid, nameof(OpenDefaultSpreadsheet), "Open in browser", GUIAlwaysEnabled = true)]
        [SerializeField]
        private string _DefaultSpreadsheetId;

        [EnhancedBoxGroup("Defaults")]
        [LabelText("Columns")]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = nameof(DrawDefaultColumnsTitleBar))]
        [SerializeReference]
        private List<SheetColumn> _DefaultColumns = new();

        [PropertyOrder(10)]
        [EnhancedBoxGroup("Tables", Color = "@ColorTools.Cyan", SpaceBefore = 6)]
        [ShowInInspector]
        [HideLabel]
        [OnStateUpdate(nameof(EnsureRows))]
        [EnhancedTableList(IsReadOnly = true, ShowFoldout = false, DrawScrollView = false, ShowPaging = false, OnTitleBarGUI = nameof(DrawTablesTitleBar))]
        [NonSerialized]
        private List<TableRow> _tables;

        [PropertyOrder(20)]
        [EnhancedBoxGroup("Selection", LabelText = "$" + nameof(SelectionTitle), Color = "@ColorTools.Aqua", SpaceBefore = 6)]
        [HorizontalGroup("Selection/Sync")]
        [IconButton("Pull (Merge)", FontAwesomeEditorIconType.DownloadSolid, "Adds and updates keys from the sheets, keeps keys missing in them.")]
        [EnableIf(nameof(CanSync))]
        private void PullSelected() => GoogleSheetsSync.PullAsync(Selected.ToList(), false).Forget();

        [PropertyOrder(21)]
        [HorizontalGroup("Selection/Sync")]
        [IconButton("Pull (Override)", FontAwesomeEditorIconType.DownloadSolid, "Mirrors the sheets, deletes keys missing in them.")]
        [GUIColor(1f, 0.75f, 0.65f)]
        [EnableIf(nameof(CanSync))]
        private void PullOverrideSelected()
        {
            if (EditorUtility.DisplayDialog("Pull (Override)", "Keys missing from the sheets will be deleted from Unity. Continue?", "Pull", "Cancel"))
                GoogleSheetsSync.PullAsync(Selected.ToList(), true).Forget();
        }

        [PropertyOrder(22)]
        [HorizontalGroup("Selection/Sync")]
        [IconButton("Push", FontAwesomeEditorIconType.UploadSolid, "Writes the tables to the sheets.")]
        [EnableIf(nameof(CanSync))]
        private void PushSelected()
        {
            if (EditorUtility.DisplayDialog("Push", "Mapped sheet columns will be overwritten. Continue?", "Push", "Cancel"))
                GoogleSheetsSync.PushAsync(Selected.ToList()).Forget();
        }

        [PropertyOrder(23)]
        [VerticalGroup("Selection/SetupSpace", PaddingTop = 4)]
        [HorizontalGroup("Selection/SetupSpace/Setup", Width = 110)]
        [Button("Apply Defaults")]
        [Tooltip("Links selected tables to the default spreadsheet, a sheet per table, with the default columns.")]
        [EnableIf("@" + nameof(HasSelection) + " && " + nameof(HasSpreadsheet))]
        private void ApplyDefaultsToSelected() => ApplyDefaultsAsync(Selected.ToList()).Forget();

        [PropertyOrder(24)]
        [HorizontalGroup("Selection/SetupSpace/Setup")]
        [ShowInInspector]
        [LabelText("Copy columns from")]
        [LabelWidth(120)]
        [EnhancedValueDropdown(nameof(GetLinkedCollections))]
        private StringTableCollection _copySource;

        [PropertyOrder(25)]
        [HorizontalGroup("Selection/SetupSpace/Setup", Width = 60)]
        [Button("Copy")]
        [EnableIf("@" + nameof(_copySource) + " != null && " + nameof(HasSelection))]
        private void CopyColumnsToSelected()
        {
            var source = _copySource.GetSheetExtensions().FirstOrDefault();
            if (source == null)
                return;

            foreach (var collection in Selected.Where(c => c != _copySource))
            {
                foreach (var extension in collection.GetSheetExtensions())
                {
                    Undo.RecordObject(collection, "Copy Columns");
                    extension.Columns.Clear();
                    extension.Columns.AddRange(IdSheetColumnUtility.Clone(source.Columns));
                    EditorUtility.SetDirty(collection);
                }
            }
        }

        private bool HasSpreadsheet => _DefaultSheetsServiceProvider != null && !string.IsNullOrWhiteSpace(_DefaultSpreadsheetId);
        private bool HasSelection => Tables.Any(r => r.Selected);
        private bool CanSync => HasSelection && !GoogleSheetsSync.IsRunning;
        private IEnumerable<StringTableCollection> Selected => Tables.Where(r => r.Selected).Select(r => r.Collection);
        private string SelectionTitle => $"Selected ({Tables.Count(r => r.Selected)})";

        [OnInspectorInit]
        private void LoadSheetNames()
        {
            foreach (var extension in LocalizationEditorSettings.GetStringTableCollections().SelectMany(c => c.GetSheetExtensions()))
                EnhancedGoogleSheetsExtension.LoadSheetsOnce(extension.SheetsServiceProvider, extension.SpreadsheetId);
        }

        private List<TableRow> Tables => _tables ??= CreateRows();

        private void EnsureRows() => _tables ??= CreateRows();

        private static List<TableRow> CreateRows() => LocalizationEditorSettings.GetStringTableCollections().Select(c => new TableRow(c)).ToList();

        private void RefreshRows()
        {
            var unselected = Tables.Where(r => !r.Selected).Select(r => r.Collection).ToHashSet();
            _tables = CreateRows();
            foreach (var row in _tables)
                row.Selected = !unselected.Contains(row.Collection);
        }

        private IEnumerable<ValueDropdownItem<StringTableCollection>> GetLinkedCollections()
        {
            yield return new ValueDropdownItem<StringTableCollection>("Choose table…", null);
            foreach (var row in Tables.Where(r => r.Collection.GetSheetExtensions().Any()))
                yield return new ValueDropdownItem<StringTableCollection>(row.Collection.TableCollectionName, row.Collection);
        }

        private void DrawTablesTitleBar()
        {
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("All")))
                Tables.ForEach(r => r.Selected = true);
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("None")))
                Tables.ForEach(r => r.Selected = false);
            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.ArrowsRotateSolid, "Reload tables"))
                RefreshRows();
        }

        private void DrawDefaultColumnsTitleBar()
        {
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Default", "Key plus a column for each project locale")))
            {
                _DefaultColumns.Clear();
                _DefaultColumns.AddRange(IdSheetColumnUtility.CreateDefaultColumns());
                EditorUtility.SetDirty(this);
            }
        }

        private async UniTaskVoid ApplyDefaultsAsync(List<StringTableCollection> collections)
        {
            var sheets = await EnhancedGoogleSheetsExtension.LoadSheetsAsync(_DefaultSheetsServiceProvider, _DefaultSpreadsheetId);
            if (sheets == null)
                return;

            var google = new UnitySheets(_DefaultSheetsServiceProvider) {SpreadSheetId = _DefaultSpreadsheetId};
            try
            {
                for (var i = 0; i < collections.Count; i++)
                {
                    var collection = collections[i];
                    EditorUtility.DisplayProgressBar("Apply Defaults", collection.TableCollectionName, i / (float) collections.Count);

                    var existing = collection.GetSheetExtensions().FirstOrDefault();
                    var sheetName = collection.TableCollectionName;
                    var sheetId = existing != null && existing.SpreadsheetId == _DefaultSpreadsheetId
                        ? existing.SheetId
                        : sheets.TryGetFirst(s => s.name == sheetName, out var sheet)
                            ? sheet.id
                            : google.AddSheet(sheetName, _DefaultSheetsServiceProvider.NewSheetProperties);

                    Undo.RecordObject(collection, "Apply Defaults");
                    foreach (var extension in collection.GetSheetExtensions().ToList())
                        collection.RemoveExtension(extension);

                    var enhanced = new EnhancedGoogleSheetsExtension
                    {
                        SheetsServiceProvider = _DefaultSheetsServiceProvider,
                        SpreadsheetId = _DefaultSpreadsheetId,
                        SheetId = sheetId
                    };
                    enhanced.Columns.AddRange(IdSheetColumnUtility.Clone(_DefaultColumns));
                    collection.AddExtension(enhanced);
                    EditorUtility.SetDirty(collection);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RefreshRows();
            }
        }

        private string SignInLabel => _DefaultSheetsServiceProvider.IsSignedIn() ? "Log Out" : "Log In";

        private void ToggleSignIn()
        {
            if (_DefaultSheetsServiceProvider.IsSignedIn())
                GoogleSheetsSync.DeleteOAuthToken(_DefaultSheetsServiceProvider);
            else
                _DefaultSheetsServiceProvider.TryConnectAsync().Forget();
        }

        private void OpenDefaultSpreadsheet()
        {
            if (!string.IsNullOrWhiteSpace(_DefaultSpreadsheetId))
                UnitySheets.OpenSheetInBrowser(_DefaultSpreadsheetId);
        }

        private class TableRow
        {
            [EnhancedTableColumn(" ", 22)]
            [HideLabel]
            [ShowInInspector]
            public bool Selected = true;

            [ShowInInspector]
            [EnhancedTableColumn(MinWidth = 110)]
            public StringTableCollection Collection { get; }

            [ShowInInspector]
            [DisplayAsString]
            [EnhancedTableColumn(MinWidth = 100)]
            [InlineIconButton(FontAwesomeEditorIconType.UpRightFromSquareSolid, nameof(Open), "Open sheet", GUIAlwaysEnabled = true, ShowIf = nameof(IsLinked))]
            private string Sheet
            {
                get
                {
                    var extensions = Collection.GetSheetExtensions().ToList();
                    if (extensions.Count == 0)
                        return "not linked";

                    return string.Join(", ", extensions.Select(e =>
                    {
                        var name = EnhancedGoogleSheetsExtension.GetCachedSheetName(e.SpreadsheetId, e.SheetId) ?? $"gid {e.SheetId}";
                        return e is EnhancedGoogleSheetsExtension ? name : $"{name} (legacy)";
                    }));
                }
            }

            [ShowInInspector]
            [DisplayAsString]
            [EnhancedTableColumn(MinWidth = 120)]
            private string Columns => string.Join("  ", Collection.GetSheetExtensions().SelectMany(e => e.Columns)
                .Select(c => c is IIdSheetColumn id ? id.Id : c.Column)
                .Where(c => !string.IsNullOrEmpty(c)));

            private bool IsLinked => Collection.GetSheetExtensions().Any();

            public TableRow(StringTableCollection collection) => Collection = collection;

            [EnhancedTableColumn(" ", 72)]
            [HorizontalGroup("Actions")]
            [IconButton(FontAwesomeEditorIconType.DownloadSolid, "Pull (Merge)")]
            [EnableIf("@" + nameof(IsLinked) + " && !" + nameof(GoogleSheetsSync) + "." + nameof(GoogleSheetsSync.IsRunning))]
            private void Pull() => GoogleSheetsSync.PullAsync(new[] {Collection}, false).Forget();

            [HorizontalGroup("Actions")]
            [IconButton(FontAwesomeEditorIconType.DownloadSolid, "Pull (Override), deletes keys missing in the sheet")]
            [GUIColor(1f, 0.75f, 0.65f)]
            [EnableIf("@" + nameof(IsLinked) + " && !" + nameof(GoogleSheetsSync) + "." + nameof(GoogleSheetsSync.IsRunning))]
            private void PullOverride()
            {
                if (EditorUtility.DisplayDialog("Pull (Override)", $"Keys missing from the sheet will be deleted from {Collection.TableCollectionName}. Continue?", "Pull", "Cancel"))
                    GoogleSheetsSync.PullAsync(new[] {Collection}, true).Forget();
            }

            [HorizontalGroup("Actions")]
            [IconButton(FontAwesomeEditorIconType.UploadSolid, "Push")]
            [EnableIf("@" + nameof(IsLinked) + " && !" + nameof(GoogleSheetsSync) + "." + nameof(GoogleSheetsSync.IsRunning))]
            private void Push()
            {
                if (EditorUtility.DisplayDialog("Push", $"Mapped columns of {Collection.TableCollectionName} will be overwritten. Continue?", "Push", "Cancel"))
                    GoogleSheetsSync.PushAsync(new[] {Collection}).Forget();
            }

            private void Open()
            {
                var extension = Collection.GetSheetExtensions().FirstOrDefault();
                if (extension != null)
                    UnitySheets.OpenSheetInBrowser(extension.SpreadsheetId, extension.SheetId);
            }
        }
    }
}
