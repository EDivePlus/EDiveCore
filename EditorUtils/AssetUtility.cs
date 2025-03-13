using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils
{
    public static class AssetUtility
    {
        [MenuItem("Assets/Set Selection Dirty", false, 50000)]
        [MenuItem("GameObject/Set Selection Dirty", false, 50000)]
        private static void SetSelectionDirty(MenuCommand menuCommand)
        {
            foreach (var file in Selection.objects)
            {
                if (file == null) continue;
                EditorUtility.SetDirty(file);
                if (file is GameObject go)
                {
                    var components = go.GetComponentsInChildren<Component>();
                    foreach (var component in components)
                    {
                        if (component == null) continue;
                        EditorUtility.SetDirty(component);
                    }

                    PrefabUtility.RecordPrefabInstancePropertyModifications(go);
                }
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/Force Reserialize Assets", false, 50000)]
        [MenuItem("GameObject/Force Reserialize Assets", false, 50000)]
        private static void ForceReserializeAssets(MenuCommand menuCommand)
        {
            foreach (var file in Selection.objects)
            {
                var pathToAsset = AssetDatabase.GetAssetPath(file);
                var assetPaths = new List<string>();
                if (!string.IsNullOrEmpty(pathToAsset))
                {
                    assetPaths.Add(pathToAsset);
                }
                else
                {
                    Debug.LogWarning($"AssetDatabase.GetAssetPath didn't find asset {file.name} {file}");
                }

                AssetDatabase.ForceReserializeAssets(assetPaths);
            }
        }
    }
}
