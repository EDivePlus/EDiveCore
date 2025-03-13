using System;
using System.Linq;
using System.Reflection;
using EDIVE.EditorUtils.EditorHeaders;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.EditorUtils
{
    public static class ChangeScriptTypeUtility
    {
        [MenuItem("CONTEXT/Object/Change Script Type")]
        public static void ChangeScriptType(MenuCommand command)
        {
            var type = command.context switch
            {
                ScriptableObject _ => typeof(ScriptableObject),
                Component _ => typeof(Component),
                _ => null
            };
            if (type == null) return;

            var targets = Selection.objects.Where(o => type.IsInstanceOfType(o));
            var selector = new TypeSelector(TypeCache.GetTypesDerivedFrom(type), false);
            selector.SelectionConfirmed += selection =>
            {
                var newType = selection.FirstOrDefault();
                if (newType == null)
                    return;

                foreach (var target in targets)
                {
                    target.ChangeScriptType(newType);
                }
            };

            var field = typeof(Event).GetField("s_Current", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                if (field.GetValue(null) is Event current)
                {
                    var rect = new Rect(current.mousePosition, Vector2.zero);
                    selector.ShowInPopup(rect);
                }
            }
        }

        [MenuItem("CONTEXT/Object/Change Script Type", true)]
        public static bool ChangeScriptTypeValidate(MenuCommand command) => command.context is ScriptableObject or Component;

        [CustomEditorHeaderItem(10)]
        private static bool ChangeScriptTypeHeaderItem(Rect rect, Object[] targets)
        {
            var firstTarget = targets[0];
            if (firstTarget == null || (firstTarget.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            if (targets.Any(t => t is not ScriptableObject && t is not Component))
                return false;

            if (EditorGUI.DropdownButton(rect, GUIHelper.TempContent(FontAwesomeEditorIcons.ArrowsRepeatSolid.Highlighted, "Change Script Type"), FocusType.Passive, EditorHeaderExtender.IconButtonStyle))
            {
                var type = firstTarget switch
                {
                    ScriptableObject _ => typeof(ScriptableObject),
                    Component _ => typeof(Component),
                    _ => null
                };

                var selector = new TypeSelector(TypeCache.GetTypesDerivedFrom(type), false);
                selector.SelectionConfirmed += selection =>
                {
                    var newType = selection.FirstOrDefault();
                    if (newType == null)
                        return;

                    foreach (var target in targets)
                    {
                        target.ChangeScriptType(newType);
                    }
                };

                var popupRect = new Rect(rect.xMin, rect.yMax, 0, 0);
                selector.ShowInPopup(popupRect);
            }
            return true;
        }

        public static TTargetType ChangeScriptType<TTargetType>(this Object target, Action<TTargetType> onScriptChangedCallback = null) where TTargetType : Object => 
            ChangeScriptType(target, typeof(TTargetType), o => onScriptChangedCallback?.Invoke(o as TTargetType)) as TTargetType;
        
        public static Object ChangeScriptType(this Object target, Type targetType, Action<Object> onScriptChangedCallback = null)
        {
            if (target == null)
            {
                Debug.LogError("Target is null!");
                return null;
            }
            if (targetType == null)
            {
                Debug.LogError("Type is null!");
                return null;
            }

            if (!targetType.TryGetMonoScript(out var targetScript))
            {
                Debug.LogError($"Could not find MonoScript for {targetType.Name}!");
                return null;
            }

            var so = new SerializedObject(target);
            var scriptProperty = so.FindProperty("m_Script");
            if (scriptProperty == null)
            {
                Debug.LogError("Object has not property 'Script' to change!");
                return null;
            }
            
            AssetDatabase.SaveAssets();
            so.Update();
            scriptProperty.objectReferenceValue = targetScript;
            so.ApplyModifiedProperties();
            var result = so.targetObject;
            scriptProperty.Dispose();
            so.Dispose();
            onScriptChangedCallback?.Invoke(result);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }
    }
}
