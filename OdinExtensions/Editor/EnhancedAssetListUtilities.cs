using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor
{
  public static class EnhancedAssetUtilities
    {
        private static List<Component> componentListBuffer = new List<Component>();
        private static readonly Type[] createableAssetTypes = new Type[3]
        {
            typeof(ScriptableObject),
            typeof(MonoBehaviour),
            typeof(GameObject)
        };

        public static IEnumerable<AssetUtilities.AssetSearchResult> GetAllAssetsOfTypeWithProgress(
            Type type,
            params string[] folderPaths)
        {
            var item = new AssetUtilities.AssetSearchResult();
            if (folderPaths != null)
            {
                for (var i = 0; i < folderPaths.Length; i++)
                {
                    folderPaths[i] = folderPaths[i].Trim('/');
                    if (!folderPaths[i].StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase))
                        folderPaths[i] = "Assets/" + folderPaths[i];
                }
            }

            if (type == typeof(GameObject))
            {
                string[] assets;
                if (folderPaths != null)
                    assets = AssetDatabase.FindAssets("t:Prefab", folderPaths);
                else
                    assets = AssetDatabase.FindAssets("t:Prefab");
                var goGuids = assets;

                item.NumberOfResults = 0;
                foreach (var goGuid in goGuids)
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(goGuid)))
                    {
                        if (asset is GameObject gameObject)
                        {
                            item.CurrentIndex = item.NumberOfResults;
                            item.NumberOfResults++;
                            item.Asset = gameObject;
                            yield return item;
                        }
                    }
                }
                goGuids = null;
            }
            else if (type.InheritsFrom(typeof(Component)))
            {
                string[] assets;
                if (folderPaths != null)
                    assets = AssetDatabase.FindAssets("t:Prefab", folderPaths);
                else
                    assets = AssetDatabase.FindAssets("t:Prefab");
                var goGuids = assets;
                
                item.NumberOfResults = 0;
                foreach (var goGuid in goGuids)
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(goGuid)))
                    {
                        if (asset is GameObject gameObject)
                        {
                            gameObject.GetComponents(type, componentListBuffer);
                            item.CurrentIndex = item.NumberOfResults;
                            item.NumberOfResults++;
                            for (var j = 0; j < componentListBuffer.Count; ++j)
                            {
                                item.Asset = componentListBuffer[j];
                                yield return item;
                            }
                        }
                    }
                }
                goGuids = null;
            }
            else
            {
                var str = type.FullName.StartsWith("UnityEngine.") || type.FullName.StartsWith("UnityEditor.") ? type.Name : type.FullName;
                string[] assets;
                if (folderPaths != null)
                {
                    var paths = folderPaths.Where(AssetDatabase.IsValidFolder);
                    assets = AssetDatabase.FindAssets("t:" + str, paths.ToArray());
                }
                else
                    assets = AssetDatabase.FindAssets("t:" + str);
                var guids = assets;
                
                item.NumberOfResults = 0;
                foreach (var guid in guids)
                {
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        if (type.IsInstanceOfType(asset))
                        {
                            item.CurrentIndex = item.NumberOfResults;
                            item.NumberOfResults++;
                            item.Asset = asset;
                            yield return item;
                        }
                    }
                }
                
                guids = null;
                
            }
        }
        
        public static bool CanCreateNewAsset<T>()
        {
            foreach (var type in createableAssetTypes)
            {
                if (typeof(T).InheritsFrom(type))
                    return true;
            }

            return false;
        }
        
        public static string GetAssetLocation(Object obj)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            return assetPath.Substring(0, assetPath.LastIndexOf('/'));
        }
        
        public static void CreateNewAsset<T>(string path, string assetName) where T : Object
        {
            if (!AssetUtilities.CanCreateNewAsset<T>(out var baseType))
            {
                Debug.LogError("Unable to create new asset of type " + typeof(T).GetNiceName());
            }
            else
            {
                if (path == null)
                {
                    path = "";
                }
                else
                {
                    path = path.Trim().TrimStart('/').TrimEnd('/').Trim();
                    if (path.ToLower(CultureInfo.InvariantCulture).StartsWith("assets", StringComparison.InvariantCulture))
                        path = path.Substring(6, path.Length - 6).TrimStart('/');
                }

                var path1 = Application.dataPath + "/" + path;
                if (!Directory.Exists(path1))
                    Directory.CreateDirectory(path1);
                assetName ??= typeof(T).GetNiceName();
                if (assetName.IndexOf('.') < 0)
                    assetName = assetName + "." + GetAssetFileExtensionName(baseType);
                var assetFolderPath = "Assets/" + path;
                var assetPath = AssetDatabase.GenerateUniqueAssetPath(assetFolderPath + "/" + assetName);
                
                if (baseType == typeof(ScriptableObject))
                {
                    OdinExtensionUtils.DrawSubtypeDropDownOrCall(typeof(T), CreateInstance);
                    void CreateInstance(Type type)
                    {
                        DelayUntilEditorUpdate(() =>
                        {
                            var result = OdinExtensionUtils.CreateNewInstanceOfType<ScriptableObject>(type, assetFolderPath) as ScriptableObject;
                            if (result != null)
                            {
                                EditorGUIUtility.PingObject(result);
                            }
                        });
                    }
                    return;
                }

                GameObject go = null;
                if (baseType == typeof(MonoBehaviour))
                {
                    var gameObject = new GameObject();
                    gameObject.AddComponent(typeof(T));
                    go = gameObject;
                }
                else
                {
                    if (baseType != typeof(GameObject))
                        throw new NotImplementedException();
                }

                var asset = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(asset);
                Object.DestroyImmediate(go);
            }
        }

        private static void DelayUntilEditorUpdate(Action action)
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            return;

            void OnUpdate()
            {
                EditorApplication.update -= OnUpdate;
                action?.Invoke();
            }
        }
        
        private static string GetAssetFileExtensionName(Type type)
        {
            if (type == typeof(ScriptableObject))
                return "asset";
            return type == typeof(GameObject) || type == typeof(MonoBehaviour) ? "prefab" : null;
        }
    }
}
