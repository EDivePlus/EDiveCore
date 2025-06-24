using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Modules.Localization.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;

namespace EDIVE.Localization.Editor
{
    [GlobalConfig("Assets/_Project/Settings/Editor/")]
    public class LocalizationConfig : GlobalConfig<LocalizationConfig>
    {
        [EnhancedBoxGroup("Google Sheets Defaults", Color = "@ColorTools.Green")]
        [ShowCreateNew]
        [LabelText("Sheets Service Provider")]
        [SerializeField]
        private SheetsServiceProvider _DefaultSheetsServiceProvider;

        [EnhancedBoxGroup("Google Sheets Defaults")]
        [LabelText("Spreadsheet ID")]
        [SerializeField]
        private string _DefaultSpreadsheetId;

        [EnhancedBoxGroup("Google Sheets Defaults")]
        [LabelText("Columns Mapping")]
        [SerializeReference]
        private List<SheetColumn> _DefaultColumns = new();

        [PropertySpace(5)]
        [EnhancedBoxGroup("Google Sheets Defaults")]
        [Button]
        [Tooltip("If you've already set up a Google Sheets Extension for a String Table Collection, you can use this to fill out the defaults from that extension.")]
        private void FillOutDefaultsFromTableCollection(StringTableCollection collection)
        {
            if (collection.Extensions.TryGetFirst(e => e is GoogleSheetsExtension, out var existingExtension) && existingExtension is GoogleSheetsExtension source)
            {
                _DefaultSheetsServiceProvider = source.SheetsServiceProvider;
                _DefaultSpreadsheetId = source.SpreadsheetId;
                _DefaultColumns.AddRange(source.Columns);
            }
            else
            {
                EditorUtility.DisplayDialog("No Google Sheets Extension", "The selected collection does not have a Google Sheets Extension", "OK");
            }
        }

        [EnhancedBoxGroup("Google Sheets Defaults")]
        [Button]
        private void ApplyDefaultsToAllTableCollections()
        {
            try
            {
                var collections = LocalizationEditorSettings.GetStringTableCollections();
                for (var index = 0; index < collections.Count; index++)
                {
                    var collection = collections[index];
                    EditorUtility.DisplayProgressBar($"Applying Google Sheets extension setting to {collection.TableCollectionName}", string.Empty, index / (float) collections.Count);

                    if (collection.Extensions.TryGetFirst(e => e is GoogleSheetsExtension, out var existingExtension))
                    {
                        collection.RemoveExtension(existingExtension);
                    }

                    var sheetName = collection.TableCollectionName;
                    var google = new GoogleSheets(_DefaultSheetsServiceProvider)
                    {
                        SpreadSheetId = _DefaultSpreadsheetId
                    };

                    if (!TryGetSheetId(google, sheetName, out var sheetId))
                    {
                        sheetId = google.AddSheet(sheetName, _DefaultSheetsServiceProvider.NewSheetProperties);
                    }

                    var extensionInstance = new GoogleSheetsExtension()
                    {
                        SheetsServiceProvider = _DefaultSheetsServiceProvider,
                        SpreadsheetId = _DefaultSpreadsheetId,
                        SheetId = sheetId
                    };
                    extensionInstance.Columns.AddRange(_DefaultColumns);
                    collection.AddExtension(extensionInstance);

                    EditorUtility.SetDirty(collection);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool TryGetSheetId(GoogleSheets google, string sheetName, out int result)
        {
            if(google.GetSheets().TryGetFirst(s => s.name == sheetName, out var sheet))
            {
                result = sheet.id;
                return true;
            }

            result = 0;
            return false;
        }

#if !ODIN_IS_UPDATING
        [VerticalGroup("Edit", PaddingTop = 6)]
        [HorizontalGroup("Edit/Tables")]
        [IconButton("Localization Editor", FontAwesomeEditorIconType.GlobeSolid)]
        private void OpenLocalizationEditor() => OdinLocalizationEditorWindow.OpenFromMenu();
#endif

        [HorizontalGroup("Edit/Tables")]
        [IconButton("Google Sheets", FontAwesomeEditorIconType.FileSpreadsheetRegular)]
        private void OpenInGoogleSheets()
        {
            if (string.IsNullOrWhiteSpace(_DefaultSpreadsheetId))
            {
                EditorUtility.DisplayDialog("No Spreadsheet ID", "You need to set the default Spreadsheet ID in the LocalizationConfig", "OK");
                return;
            }

            GoogleSheets.OpenSheetInBrowser(_DefaultSpreadsheetId);
        }

        [HorizontalGroup("Edit/PushPull")]
        [IconButton("Pull (Import)", FontAwesomeEditorIconType.DownloadSolid)]
        private void PullAll()
        {
            PullAllInternal().Forget();
            return;

            async UniTaskVoid PullAllInternal()
            {
                try
                {
                    var collections = LocalizationEditorSettings.GetStringTableCollections();
                    var progressBarReporter = new BatchProgressBarReporter(collections.Count);

                    await _DefaultSheetsServiceProvider.TryConnectAsync();

                    // Its possible a String Table Collection may have more than one GoogleSheetsExtension.
                    // For example if each Locale we pushed/pulled from a different sheet.
                    for (var i = 0; i < collections.Count; i++)
                    {
                        var collection = collections[i];

                        collection.ClearAllEntries();
                        progressBarReporter.SetBatch(i, collections.Count);

                        for (var j = 0; j < collection.Extensions.Count; j++)
                        {
                            var extension = collection.Extensions[j];
                            progressBarReporter.SetIndexWithinBatch(j);

                            if (extension is not GoogleSheetsExtension googleExtension)
                                continue;

                            // Setup the connection to Google
                            var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider)
                            {
                                SpreadSheetId = googleExtension.SpreadsheetId
                            };

                            // Now update the collection. We can pass in an optional ProgressBarReporter so that we can updates in the Editor.
                            googleSheets.PullIntoStringTableCollection(googleExtension.SheetId, googleExtension.TargetCollection as StringTableCollection, googleExtension.Columns,
                                reporter: progressBarReporter);
                        }
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        [HorizontalGroup("Edit/PushPull")]
        [IconButton("Push (Export)", FontAwesomeEditorIconType.UploadSolid)]
        private void PushAll()
        {
            PushAllInternal().Forget();
            return;

            async UniTaskVoid PushAllInternal()
            {
                try
                {
                    var collections = LocalizationEditorSettings.GetStringTableCollections();
                    var progressBarReporter = new BatchProgressBarReporter(collections.Count);

                    await _DefaultSheetsServiceProvider.TryConnectAsync();

                    // Its possible a String Table Collection may have more than one GoogleSheetsExtension.
                    // For example if each Locale we pushed/pulled from a different sheet.
                    for (var i = 0; i < collections.Count; i++)
                    {
                        var collection = collections[i];
                        progressBarReporter.SetBatch(i, collections.Count);

                        for (var j = 0; j < collection.Extensions.Count; j++)
                        {
                            var extension = collection.Extensions[j];
                            progressBarReporter.SetIndexWithinBatch(j);

                            if (extension is not GoogleSheetsExtension googleExtension)
                                continue;

                            // Setup the connection to Google
                            var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider)
                            {
                                SpreadSheetId = googleExtension.SpreadsheetId
                            };

                            // Now send the update. We can pass in an optional ProgressBarReporter so that we can updates in the Editor.
                            await googleSheets.PushStringTableCollectionAsync(googleExtension.SheetId, googleExtension.TargetCollection as StringTableCollection, googleExtension.Columns, progressBarReporter);
                        }
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        [PropertySpace]
        [VerticalGroup("Edit")]
        [IconButton("Delete OAuth2 Token", FontAwesomeEditorIconType.TrashSolid)]
        public void DeleteOAuthToken()
        {
            _DefaultSheetsServiceProvider.DataStore = null;
            var dir = new DirectoryInfo(Path.Combine(Application.dataPath, $"../Library/Google/{_DefaultSheetsServiceProvider.name}"));
            if (dir.Exists)
            {
                dir.Delete(true);
            }
        }
    }
}
