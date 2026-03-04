// Author: František Holubec
// Created: 04.03.2026

using TMPro;
using UnityEditor;

namespace EDIVE.EditorUtils
{
    public static class TMPEditorUtils
    {
        [MenuItem("Tools/TMP/Clear Selected Fonts Data")]
        private static void ClearDynamicData()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is not TMP_FontAsset font)
                    continue;

                font.atlasPopulationMode = AtlasPopulationMode.Static;
                font.ClearFontAssetData(true);
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                EditorUtility.SetDirty(font);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
