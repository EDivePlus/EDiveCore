using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils
{
    // TMP font assets save their dynamic data to the same file as config, polluting VCS.
    // This clears any TMP dynamic font asset data before they are saved, preventing polluting VCS.
    // Based on https://forum.unity.com/threads/tmpro-dynamic-font-asset-constantly-changes-in-source-control.1227831/#post-8934711
    internal class DynamicFontAssetAutoCleaner : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                try
                {
                    var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (assetType == null)
                        continue;
                    
                    if (assetType != typeof(TMP_FontAsset) && !assetType.IsSubclassOf(typeof(TMP_FontAsset)))
                        continue;
                    
                    var fontAsset = AssetDatabase.LoadMainAssetAtPath(path) as TMP_FontAsset;
                    if (fontAsset == null)
                        continue;

                    var isDynamic = fontAsset.atlasPopulationMode is AtlasPopulationMode.Dynamic or AtlasPopulationMode.DynamicOS;
                    if (!isDynamic)
                        continue;
                    
                    fontAsset.ClearFontAssetData(setAtlasSizeToZero: true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError($"Something went wrong while clearing dynamic font data. For more info look at previous log message. Font asset path: '{path}'");
                }
            }

            return paths;
        }
    }
}
