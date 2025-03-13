using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 100, 0)]
    public class RichTextLabelAttributeDrawer : OdinAttributeDrawer<RichTextLabelAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, new GUIStyle(SirenixGUIStyles.Label){richText = true}, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            CallNextDrawer(null);
            EditorGUILayout.EndHorizontal();
        }
    }
}
