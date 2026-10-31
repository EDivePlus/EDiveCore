// Author: František Holubec
// Created: 29.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;
using UnityEngine.Localization;
using UnitySheets = UnityEditor.Localization.Plugins.Google.GoogleSheets;

namespace EDIVE.Localization.Editor.Sheets
{
    [Serializable]
    [StringTableCollectionExtension]
    [DisplayName("Google Sheets (Enhanced)")]
    [TypeRegistryItem("Google Sheets (Enhanced)")]
    public class EnhancedGoogleSheetsExtension : GoogleSheetsExtension
    {
        private static readonly Dictionary<string, List<(string name, int id)>> SHEETS_CACHE = new();
        private static readonly HashSet<string> SHEETS_REQUESTED = new();

        [ShowInInspector]
        [LabelText("Provider")]
        [Required]
        private SheetsServiceProvider Provider { get => SheetsServiceProvider; set => SheetsServiceProvider = value; }

        [ShowInInspector]
        [LabelText("Spreadsheet")]
        [Required]
        [InlineIconButton(FontAwesomeEditorIconType.UpRightFromSquareSolid, nameof(OpenInBrowser), "Open in browser", GUIAlwaysEnabled = true)]
        private string Spreadsheet { get => SpreadsheetId; set => SpreadsheetId = value; }

        [ShowInInspector]
        [LabelText("Sheet")]
        [EnhancedValueDropdown(nameof(GetSheetItems), ValueLabelGetter = nameof(GetSheetLabel))]
        [InlineIconButton(FontAwesomeEditorIconType.ArrowsRotateSolid, nameof(LoadSheets), "Load sheets")]
        [OnInspectorInit(nameof(AutoLoadSheets))]
        private int Sheet { get => SheetId; set => SheetId = value; }

        [ShowInInspector]
        [LabelText("Columns")]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = nameof(DrawColumnsTitleBar))]
        private List<SheetColumn> ColumnsList
        {
            get => Columns;
            set
            {
                if (value == Columns) return;
                Columns.Clear();
                Columns.AddRange(value);
            }
        }

        private StringTableCollection Collection => TargetCollection as StringTableCollection;
        private bool CanSync => !GoogleSheetsSync.IsRunning && Collection != null && SheetsServiceProvider != null && !string.IsNullOrEmpty(SpreadsheetId);

        [VerticalGroup("SyncSpace", PaddingTop = 4)]
        [HorizontalGroup("SyncSpace/Sync")]
        [IconButton("Pull (Merge)", FontAwesomeEditorIconType.DownloadSolid, "Adds and updates keys from the sheet, keeps keys missing in it.")]
        [EnableIf(nameof(CanSync))]
        private void Pull() => GoogleSheetsSync.PullAsync(new[] {Collection}, false).Forget();

        [HorizontalGroup("SyncSpace/Sync")]
        [IconButton("Pull (Override)", FontAwesomeEditorIconType.DownloadSolid, "Mirrors the sheet, deletes keys missing in it.")]
        [GUIColor(1f, 0.75f, 0.65f)]
        [EnableIf(nameof(CanSync))]
        private void PullOverride()
        {
            if (EditorUtility.DisplayDialog("Pull (Override)", $"Keys missing from the sheet will be deleted from {Collection.TableCollectionName}. Continue?", "Pull", "Cancel"))
                GoogleSheetsSync.PullAsync(new[] {Collection}, true).Forget();
        }

        [HorizontalGroup("SyncSpace/Sync")]
        [IconButton("Push", FontAwesomeEditorIconType.UploadSolid, "Writes the table to the sheet.")]
        [EnableIf(nameof(CanSync))]
        private void Push()
        {
            if (EditorUtility.DisplayDialog("Push", "Mapped sheet columns will be overwritten. Continue?", "Push", "Cancel"))
                GoogleSheetsSync.PushAsync(new[] {Collection}).Forget();
        }

        private void OpenInBrowser()
        {
            if (string.IsNullOrEmpty(SpreadsheetId))
                return;
            UnitySheets.OpenSheetInBrowser(SpreadsheetId, SheetId);
        }

        private void LoadSheets() => LoadSheetsAsync(SheetsServiceProvider, SpreadsheetId).Forget();

        private void AutoLoadSheets() => LoadSheetsOnce(SheetsServiceProvider, SpreadsheetId);

        // Names for the sheet dropdowns, fetched once per spreadsheet and editor session, only when already signed in
        public static void LoadSheetsOnce(SheetsServiceProvider provider, string spreadsheetId)
        {
            if (provider.IsSignedIn() && !string.IsNullOrEmpty(spreadsheetId) && SHEETS_REQUESTED.Add(spreadsheetId))
                LoadSheetsAsync(provider, spreadsheetId).Forget();
        }

        public static async UniTask<List<(string name, int id)>> LoadSheetsAsync(SheetsServiceProvider provider, string spreadsheetId)
        {
            if (provider == null || string.IsNullOrEmpty(spreadsheetId))
                return null;

            try
            {
                var sheets = await GoogleSheetsSync.GetSheetsAsync(provider, spreadsheetId);
                SHEETS_CACHE[spreadsheetId] = sheets;
                return sheets;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        public static string GetCachedSheetName(string spreadsheetId, int sheetId) =>
            spreadsheetId != null && SHEETS_CACHE.TryGetValue(spreadsheetId, out var sheets) && sheets.Any(s => s.id == sheetId)
                ? sheets.First(s => s.id == sheetId).name
                : null;

        private IEnumerable<ValueDropdownItem> GetSheetItems()
        {
            if (SpreadsheetId != null && SHEETS_CACHE.TryGetValue(SpreadsheetId, out var sheets))
                return sheets.Select(s => new ValueDropdownItem(s.name, s.id));
            return new[] {new ValueDropdownItem(GetSheetLabel(SheetId), SheetId)};
        }

        private string GetSheetLabel(int value) => GetCachedSheetName(SpreadsheetId, value) ?? $"gid {value}";

        private void DrawColumnsTitleBar()
        {
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Default", "Key plus a column for each project locale")))
            {
                Undo.RecordObject(TargetCollection, "Default Columns");
                Columns.Clear();
                Columns.AddRange(IdSheetColumnUtility.CreateDefaultColumns());
                EditorUtility.SetDirty(TargetCollection);
            }
        }
    }

    public class EnhancedGoogleSheetsExtensionAttributeProcessor : OdinAttributeProcessor<EnhancedGoogleSheetsExtension>
    {
        // Raw base fields are replaced by the proxies
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member.DeclaringType != typeof(EnhancedGoogleSheetsExtension))
                attributes.Add(new HideInInspector());
        }
    }
}
