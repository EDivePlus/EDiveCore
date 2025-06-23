using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class TagSelectorAttributeDrawer : OdinAttributeDrawer<TagSelectorAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueEntry.SmartValue = EditorGUILayout.TagField(label, ValueEntry.SmartValue);
        }
    }
}
