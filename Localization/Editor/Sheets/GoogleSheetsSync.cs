// Author: František Holubec
// Created: 29.10.2026

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;
using UnitySheets = UnityEditor.Localization.Plugins.Google.GoogleSheets;

namespace EDIVE.Localization.Editor.Sheets
{
    public static class GoogleSheetsSync
    {
        public static bool IsRunning { get; private set; }

        public static IEnumerable<GoogleSheetsExtension> GetSheetExtensions(this StringTableCollection collection) =>
            collection.Extensions.OfType<GoogleSheetsExtension>();

        public static UnitySheets CreateSheets(this GoogleSheetsExtension extension) =>
            new(extension.SheetsServiceProvider) {SpreadSheetId = extension.SpreadsheetId};

        public static async UniTask PullAsync(IList<StringTableCollection> collections, bool removeMissing)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            try
            {
                var reporter = new BatchProgressBarReporter(collections.Count);
                for (var i = 0; i < collections.Count; i++)
                {
                    var collection = collections[i];
                    var extensions = collection.GetSheetExtensions().ToList();
                    reporter.SetBatch(i, extensions.Count);
                    if (extensions.Count == 0)
                        continue;

                    // Several sheets feed one table, clear once up front
                    if (removeMissing && extensions.Count > 1)
                        collection.ClearAllEntries();

                    for (var j = 0; j < extensions.Count; j++)
                    {
                        reporter.SetIndexWithinBatch(j);
                        var extension = extensions[j];
                        await extension.SheetsServiceProvider.TryConnectAsync();
                        var sheets = extension.CreateSheets();
                        var columns = ResolveColumns(sheets, extension, false);
                        sheets.PullIntoStringTableCollection(extension.SheetId, collection, columns,
                            removeMissingEntries: removeMissing && extensions.Count == 1, reporter: reporter);
                    }
                }
            }
            finally
            {
                IsRunning = false;
                EditorUtility.ClearProgressBar();
            }
        }

        public static async UniTask PushAsync(IList<StringTableCollection> collections)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            try
            {
                var reporter = new BatchProgressBarReporter(collections.Count);
                for (var i = 0; i < collections.Count; i++)
                {
                    var collection = collections[i];
                    var extensions = collection.GetSheetExtensions().ToList();
                    reporter.SetBatch(i, extensions.Count);
                    for (var j = 0; j < extensions.Count; j++)
                    {
                        reporter.SetIndexWithinBatch(j);
                        var extension = extensions[j];
                        await extension.SheetsServiceProvider.TryConnectAsync();
                        var sheets = extension.CreateSheets();
                        var columns = ResolveColumns(sheets, extension, true);
                        await sheets.PushStringTableCollectionAsync(extension.SheetId, collection, columns, reporter);
                    }
                }
            }
            finally
            {
                IsRunning = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private static List<SheetColumn> ResolveColumns(UnitySheets sheets, GoogleSheetsExtension extension, bool forPush)
        {
            if (!extension.Columns.Any(c => c is IIdSheetColumn))
                return extension.Columns;

            return IdSheetColumnUtility.Resolve(extension.Columns, sheets.GetHeaders(extension.SheetId), forPush, extension.TargetCollection != null ? extension.TargetCollection.TableCollectionName : extension.SheetId.ToString());
        }

        // Header row text and notes, notes hold the column IDs
        public static List<SheetHeader> GetHeaders(this UnitySheets sheets, int sheetId)
        {
            var request = sheets.SheetsService.Service.Spreadsheets.GetByDataFilter(new GetSpreadsheetByDataFilterRequest
            {
                DataFilters = new List<DataFilter> {new() {GridRange = new GridRange {SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1}}},
                IncludeGridData = true
            }, sheets.SpreadSheetId);
            request.Fields = "sheets(data(rowData(values(formattedValue,note))))";
            var values = request.Execute().Sheets?.FirstOrDefault()?.Data?.FirstOrDefault()?.RowData?.FirstOrDefault()?.Values;
            return values?.Select(v => new SheetHeader(v.FormattedValue, v.Note)).ToList() ?? new List<SheetHeader>();
        }

        public static async UniTask<List<(string name, int id)>> GetSheetsAsync(SheetsServiceProvider provider, string spreadsheetId)
        {
            await provider.TryConnectAsync();
            return new UnitySheets(provider) {SpreadSheetId = spreadsheetId}.GetSheets();
        }

        // Swaps legacy extensions for enhanced ones, letter columns become ID columns named after the current sheet headers
        public static async UniTask ConvertToEnhancedAsync(IList<StringTableCollection> collections)
        {
            foreach (var collection in collections)
            {
                foreach (var legacy in collection.GetSheetExtensions().Where(e => e is not EnhancedGoogleSheetsExtension).ToList())
                {
                    IList<SheetHeader> headers = null;
                    try
                    {
                        await legacy.SheetsServiceProvider.TryConnectAsync();
                        headers = legacy.CreateSheets().GetHeaders(legacy.SheetId);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Localization] {collection.TableCollectionName}: could not read sheet headers, using default titles. {e.Message}");
                    }

                    var enhanced = new EnhancedGoogleSheetsExtension
                    {
                        SheetsServiceProvider = legacy.SheetsServiceProvider,
                        SpreadsheetId = legacy.SpreadsheetId,
                        SheetId = legacy.SheetId,
                        RemoveMissingPulledKeys = legacy.RemoveMissingPulledKeys
                    };
                    enhanced.Columns.AddRange(legacy.Columns.Select(c => IdSheetColumnUtility.ToIdColumn(c, headers)));

                    Undo.RecordObject(collection, "Convert Google Sheets Extension");
                    collection.RemoveExtension(legacy);
                    collection.AddExtension(enhanced);
                    EditorUtility.SetDirty(collection);
                }
            }
        }

        public static void DeleteOAuthToken(SheetsServiceProvider provider)
        {
            if (provider == null)
                return;

            provider.DataStore = null;
            provider.ClearConnection();
            var dir = provider.GetOAuthTokenDirectory();
            if (dir.Exists)
                dir.Delete(true);
        }
    }
}
