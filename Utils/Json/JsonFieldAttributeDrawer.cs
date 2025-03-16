#if UNITY_EDITOR
using EDIVE.NativeUtils;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.Json
{
    [UsedImplicitly]
    public class JsonFieldAttributeDrawer : OdinAttributeDrawer<JsonFieldAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();

            var entry = ValueEntry;
            var attribute = Attribute;

            GUIHelper.PushGUIEnabled(false);
            var previewValue = entry.SmartValue.Truncate(attribute.MaxPreviewCharacters, true);
            if (attribute.PreviewLines > 1)
            {
                var position = EditorGUILayout.GetControlRect(label != null, EditorGUIUtility.singleLineHeight * attribute.PreviewLines);
                position.height -= 2;

                if (label == null)
                {
                    EditorGUI.TextArea(position, previewValue, EditorStyles.textArea);
                }
                else
                {
                    var controlID = GUIUtility.GetControlID(label, FocusType.Keyboard, position);
                    var areaPosition = EditorGUI.PrefixLabel(position, controlID, label, EditorStyles.label);

                    EditorGUI.TextArea(areaPosition, previewValue, EditorStyles.textArea);
                }
            }
            else
            {
                if (label == null)
                {
                    EditorGUILayout.TextField(previewValue);
                }
                else
                {
                    EditorGUILayout.TextField(label, previewValue);
                }
            }
            GUIHelper.PopGUIEnabled();

            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(rect, EditorIcons.File))
            {
                var window = ScriptableObject.CreateInstance<JsonEditWindow>();
                window.Show();
                JsonEditWindow.OpenWindow(() => entry.SmartValue, text => entry.SmartValue = text);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
