#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions
{
    public static class OdinExtensionUtils
    {
        private static Dictionary<Type, ValueDropdownList<Type>> _cachedAssignableTypes;

        public static void DrawSubtypeDropDownOrCall(Type type, Action<Type> onSelectAction)
        {
            if(type == null) onSelectAction?.Invoke(null);
            var dropdownPosition = new Rect(Event.current.mousePosition, Vector2.zero);
            DrawSubtypeDropDownOrCall(dropdownPosition, type, onSelectAction);
        }
        
        public static void DrawSubtypeDropDownOrCall(Rect dropdownPosition, Type type, Action<Type> onSelectAction)
        {
            if(type == null) onSelectAction?.Invoke(null);

            var assignableTypes = TypeCache.GetTypesDerivedFrom(type).Append(type).Where(t => !t.IsAbstract && !t.IsGenericType).ToList();
            if (assignableTypes.Count <= 0) return;
            if (assignableTypes.Count == 1)
            {
                EditorApplication.delayCall += () => onSelectAction?.Invoke(assignableTypes[0]);
                return;
            }
            
            var selector = new GenericSelector<Type>(null, false, x => $"{x.Name} ({x.Namespace})", assignableTypes);
            selector.SelectionTree.DefaultMenuStyle.Height = 22;
            selector.SelectionTree.Config.DrawSearchToolbar = true;
            selector.SelectionTree.Config.AutoFocusSearchBar = true;
            selector.SelectionTree.EnumerateTree().AddThumbnailIcons(true);
            selector.EnableSingleClickToSelect();
            
            selector.SelectionConfirmed += selection =>
            {
                var newType = selection.FirstOrDefault();
                if (newType != null)
                {
                    EditorApplication.delayCall += () => onSelectAction?.Invoke(newType);
                }
            };
            
            selector.ShowInPopup(dropdownPosition);
        }

        public static bool HasParentObject<T>(this InspectorProperty property, Predicate<T> filter = null, bool includeSelf = false)
        {
            return TryGetParentObject(property, out _, out _, filter, includeSelf);
        }

        public static T GetParentObject<T>(this InspectorProperty property, Predicate<T> filter = null, bool includeSelf = false)
        {
            TryGetParentObject(property, out var result, out _, filter, includeSelf);
            return result;
        }

        public static bool TryGetParentObject<T>(this InspectorProperty property, out T result, Predicate<T> filter = null, bool includeSelf = false)
        {
            return TryGetParentObject(property, out result, out _, filter, includeSelf);
        }
        
        public static bool TryGetParentObject<T>(this InspectorProperty property, out T result, out InspectorProperty parentProperty, Predicate<T> filter = null, bool includeSelf = false)
        {
            Func<InspectorProperty, bool> predicate = filter != null ?
                p => p?.ValueEntry?.WeakSmartValue is T tVal && filter(tVal) :
                p => p?.ValueEntry?.WeakSmartValue is T;

            parentProperty = property.FindParent(predicate, includeSelf);
            if (parentProperty?.ValueEntry.WeakSmartValue is T tResult)
            {
                result = tResult;
                return true;
            }
            result = default;
            return false;
        }

        public static void ForceMarkDirty(this InspectorProperty property, bool alsoMarkRootDirty = true)
        {
            if (property?.ValueEntry?.WeakValues == null)
                return;

            property.ValueEntry.ApplyChanges();
            property.ValueEntry.WeakValues.ForceMarkDirty();
            if (alsoMarkRootDirty) property.MarkSerializationRootDirty();
        }

        /// <summary>
        /// Create asset of type T on path with name "assetName" directly, without opening save panel
        /// </summary>
        /// <param name="path">Path for asset to be created on</param>
        /// <param name="assetName">Name without extension .asset</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateNewInstanceOfTypeDirectly<T>(string path, string assetName = null) where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            if (!Directory.Exists(path))
            {
                Debug.LogError($"Invalid path for creating asset: {path}");
                return null;
            }

            if (string.IsNullOrEmpty(assetName))
                assetName = $"new {typeof(T).Name}";
            instance.name = assetName;
            
            AssetDatabase.CreateAsset(instance, PathUtilities.Combine(path, $"{assetName}.asset"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return instance;
        }

        /// <summary>
        /// Create asset of type T on path selected in SaveFilePanel.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="defaultAssetsPath">Default path to show in SaveFilePanel</param>
        /// <param name="defaultAssetName">Name without extension .asset</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateNewInstanceOfType<T>(Type type, string defaultAssetsPath = null, string defaultAssetName = null) where T : ScriptableObject
        {
            type ??= typeof(T);
            return CreateNewInstanceOfType(type, defaultAssetsPath, defaultAssetName) as T;
        }

        public static T CreateNewInstanceOfType<T>(string defaultAssetsPath = null, string defaultAssetName = null) where T : ScriptableObject
        {
            return CreateNewInstanceOfType<T>(typeof(T), defaultAssetsPath, defaultAssetName) as T;
        }
        
        public static ScriptableObject CreateNewInstanceOfType(Type type, string defaultAssetsPath = null, string defaultAssetName = null)
        {
            var defaultPath = Application.dataPath;
            if (!string.IsNullOrWhiteSpace(defaultAssetsPath)) defaultPath = PathUtility.GetAbsolutePath(defaultAssetsPath);
            var defaultName = $"New {type.Name}";
            if (!string.IsNullOrWhiteSpace(defaultAssetName)) defaultName = defaultAssetName;
            
            var path = EditorUtility.SaveFilePanel($"Create new {type.Name}", defaultPath, defaultName, "asset");
            if (string.IsNullOrEmpty(path)) return null;
            path = $"Assets{path.Replace(Application.dataPath, "")}";
            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return instance;
        }

        public static T CreateNewGameObjectOfType<T>(Type type = null, string defaultAssetsPath = null, string defaultAssetName = null) where T : Component
        {
            type ??= typeof(T);
            return CreateNewGameObjectOfType(type, defaultAssetsPath, defaultAssetName) as T;
        }
        
        public static Component CreateNewGameObjectOfType(Type type, string defaultAssetsPath = null, string defaultAssetName = null)
        {
            var defaultPath = Application.dataPath;
            
            if (!string.IsNullOrWhiteSpace(defaultAssetsPath)) defaultPath = PathUtility.GetAbsolutePath(defaultAssetsPath);
            var defaultName = $"New {type.Name}";
            if (!string.IsNullOrWhiteSpace(defaultAssetName)) defaultName = defaultAssetName;
            
            var path = EditorUtility.SaveFilePanel($"Create new {type.Name}", defaultPath, defaultName, "prefab");
            if (string.IsNullOrEmpty(path)) return null;
            path = $"Assets{path.Replace(Application.dataPath, "")}";
            
            var instance = new GameObject("GameObject", type);
            if (!instance.TryGetComponent(type, out var component))
                return null;
            
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab.GetComponent(type);
        }
        
        public static T DuplicateAsset<T>(T asset) where T : Object
        {
            try
            {
                if (asset == null)
                    return null;
            
                var path = AssetDatabase.GetAssetPath(asset);
                if (path == null)
                {
                    Debug.LogError("Asset not found in the AssetDatabase!"); 
                    return null;
                }
                
                var filename = Path.GetFileNameWithoutExtension(path);
                var extension = Path.GetExtension(path);

                var fullDirectoryPath = Path.GetDirectoryName(PathUtility.GetAbsolutePath(path));
                if (string.IsNullOrEmpty(fullDirectoryPath))
                {
                    Debug.LogError("Directory of asset is invalid!"); 
                    return null;
                }
                
                var otherFileNames = Directory.GetFiles(fullDirectoryPath, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToArray();
                var newName = ObjectNames.GetUniqueName(otherFileNames, filename);
            
                var newPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, $"{newName}{extension}").ConvertPathSeparator();
                AssetDatabase.CopyAsset(path, newPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<T>(newPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
        
        public static bool IsAssignable(Type baseType, IEnumerable<Type> allowedTypes, bool includeChildren)
        {
            if (includeChildren)
            {
                foreach (var type in allowedTypes)
                {
                    if (type.IsAssignableFrom(baseType)) 
                        return true;
                }
            }
            else
            {
                foreach (var type in allowedTypes)
                {
                    if (baseType == type) 
                        return true;
                }
            }
            return false;
        }

        public static bool ToolbarIconButton(EditorIcon icon, string tooltip = null, bool ignoreGUIEnabled = false)
        {
            Rect rect = GUILayoutUtility.GetRect(SirenixEditorGUI.currentDrawingToolbarHeight, 0.0f, (GUILayoutOption[]) GUILayoutOptions.ExpandWidth(false).ExpandHeight());
            if (GUI.Button(rect, GUIHelper.TempContent(string.Empty, tooltip), SirenixGUIStyles.ToolbarButton))
            {
                GUIHelper.RemoveFocusControl();
                GUIHelper.RequestRepaint();
                return true;
            }
            if (Event.current.type == UnityEngine.EventType.Repaint)
                icon.Draw(rect, 16f);
            if (!ignoreGUIEnabled || Event.current.button != 0 || Event.current.rawType != UnityEngine.EventType.MouseDown || !GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                return false;
            GUIHelper.RemoveFocusControl();
            GUIHelper.RequestRepaint();
            GUIHelper.PushGUIEnabled(true);
            Event.current.Use();
            GUIHelper.PopGUIEnabled();
            return true;
        }
    }
}
#endif