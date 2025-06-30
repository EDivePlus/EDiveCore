using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public sealed class LayerFieldAttributeDrawer : OdinAttributeDrawer<LayerFieldAttribute, int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var newLayer = EditorGUILayout.LayerField(label, ValueEntry.SmartValue);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = newLayer;
                Property.MarkSerializationRootDirty();
            }
        }
    }

}
