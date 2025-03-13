using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.EditorUtils
{
    public static class EditorAssetUtils
    {
        public static string GetSelectedAssetsPath()
        {
            var path = "Assets";
            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) 
                    continue;
                
                path = Path.GetDirectoryName(path);
                break;
            }
            return path;
        }
        
        public static List<T> FindAllPrefabsOfType<T>(string[] searchInFolders = null) where T : MonoBehaviour
        {
            return FindAllPrefabsOfType<T>(typeof(T), searchInFolders);
        }

        public static List<T> FindAllPrefabsOfType<T>(Type specificType, string[] searchInFolders = null) where T : MonoBehaviour
        {
            var guids = searchInFolders == null || searchInFolders.All(f => f == null)
                ? AssetDatabase.FindAssets("t:Prefab")
                : AssetDatabase.FindAssets("t:Prefab", searchInFolders);

            if (guids == null) return new List<T>();

            var prefabs = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is GameObject go && go.TryGetComponent(specificType, out var component) && component is T tComponent)
                    {
                        prefabs.Add(tComponent);
                    }
                }
            }

            return prefabs;
        }

        public static List<T> FindAllAssetsOfType<T>(string[] searchInFolders = null, Func<string, bool> assetPathFilter = null) where T : Object
        {
            var guids = searchInFolders == null || searchInFolders.All(f => f == null)
                ? AssetDatabase.FindAssets("t:" + typeof(T).Name)
                : AssetDatabase.FindAssets("t:" + typeof(T).Name, searchInFolders);

            if (guids == null)
                return null;

            var result = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPathFilter != null && !assetPathFilter.Invoke(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T tAsset)
                    {
                        result.Add(tAsset);
                    }
                }
            }

            return result;
        }

        public static List<Object> FindAllAssetsOfType(Type targetType, string[] searchInFolders = null, Func<string, bool> assetPathFilter = null)
        {
            var guids = searchInFolders == null || searchInFolders.All(f => f == null)
                ? AssetDatabase.FindAssets("t:" + targetType.Name)
                : AssetDatabase.FindAssets("t:" + targetType.Name, searchInFolders);

            if (guids == null)
                return null;

            var result = new List<Object>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPathFilter != null && !assetPathFilter.Invoke(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (targetType.IsInstanceOfType(asset))
                    {
                        result.Add(asset);
                    }
                }
            }

            return result;
        }

        public static List<T> FindAllAssetsOfType<T, TAsset>(string[] searchInFolders = null, Func<string, bool> assetPathFilter = null) where TAsset : Object
        {
            var guids = searchInFolders == null || searchInFolders.All(f => f == null)
                ? AssetDatabase.FindAssets("t:" + typeof(TAsset).Name)
                : AssetDatabase.FindAssets("t:" + typeof(TAsset).Name, searchInFolders);

            if (guids == null)
                return null;

            var result = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPathFilter != null && !assetPathFilter.Invoke(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T tAsset)
                    {
                        result.Add(tAsset);
                    }
                }
            }

            return result;
        }

        public static List<T> FindAllAssetsOfTypeFullName<T>(string[] searchInFolders = null, Func<string, bool> assetPathFilter = null) where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).FullName, searchInFolders);
            if (guids == null)
                return null;

            var result = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPathFilter != null && !assetPathFilter.Invoke(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T tAsset)
                    {
                        result.Add(tAsset);
                    }
                }
            }

            return result;
        }

        public static void FindAllAssetsOfType<T>(HashSet<T> output, string[] searchInFolders = null) where T : Object
        {
            var guidsArray = AssetDatabase.FindAssets("t:" + typeof(T).Name, searchInFolders);
            if (guidsArray == null)
            {
                output.Clear();
                return;
            }

            var guids = new HashSet<string>(guidsArray);
            var elementsToRemove = new HashSet<T>();

            foreach (var existingElement in output)
            {
                var assetPath = AssetDatabase.GetAssetPath(existingElement);
                var guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (guids.Contains(guid))
                {
                    guids.Remove(guid);
                }
                else
                {
                    elementsToRemove.Add(existingElement);
                }
            }

            foreach (var elementToRemove in elementsToRemove)
            {
                output.Remove(elementToRemove);
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T tAsset)
                    {
                        output.Add(tAsset);
                    }
                }
            }
        }

        public static List<T> FindAllAssetsOfType<T>(Type type, string[] searchInFolders = null, Func<string, bool> assetPathFilter = null) where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + type.Name, searchInFolders);
            if (guids == null)
                return null;

            var result = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPathFilter != null && !assetPathFilter.Invoke(path)) continue;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T tAsset)
                    {
                        result.Add(tAsset);
                    }
                }
            }

            return result;
        }

        public static T FindAssetOfType<T>(bool useFullName = false, string assetName = null) where T : Object { return FindAssetOfType<T>(typeof(T), useFullName, assetName); }

        public static T FindAssetOfType<T>(Type specificType, bool useFullName = false, string assetName = null) where T : Object
        {
            string[] guids;
            var typeName = useFullName ? specificType.FullName : specificType.Name;
            if (string.IsNullOrWhiteSpace(assetName))
                guids = AssetDatabase.FindAssets($"t:{typeName}");
            else
                guids = AssetDatabase.FindAssets($"t:{typeName} {assetName}");

            if (guids == null || guids.Length < 1)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        
        public static void EnsurePath(string path)
        {
            var subfolders = path.Split('/');
            if (subfolders.Length <= 0) return;

            var partialPath = "";
            var parentPath = "";
            foreach (var subFolder in subfolders)
            {
                var currentPath = partialPath + subFolder;
                partialPath = currentPath + "/";
                if (!AssetDatabase.IsValidFolder(currentPath))
                    AssetDatabase.CreateFolder(parentPath, subFolder);
                parentPath = currentPath;
            }
        }
    }
}
