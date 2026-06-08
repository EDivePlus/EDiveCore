using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class EnhancedBoxGroupAttributeDrawer : OdinGroupDrawer<EnhancedBoxGroupAttribute>
    {
        private ValueResolver<string> _labelResolver;
        private ValueResolver<Color> _colorResolver;
        private ValueResolver<bool> _useIfResolver;
        
        protected override void Initialize()
        {
            _labelResolver = ValueResolver.GetForString(Property, Attribute.LabelText ?? Attribute.GroupName);
            _colorResolver = ValueResolver.Get(Property, Attribute.Color, Attribute.DefaultColor);
            _useIfResolver = ValueResolver.Get(Property, Attribute.UseIf, true);
        }
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors( _useIfResolver);
            if (_useIfResolver.GetValue() == false)
            {
                foreach (var child in Property.Children)
                {
                    child.Draw(child.Label);
                }
                return;
            }

            GUILayout.Space(Attribute.SpaceBefore);
            ValueResolver.DrawErrors(_labelResolver, _colorResolver);

            var previousBgColor = GUI.backgroundColor;
            if (Attribute.HasColorDefined)
            {
                var backgroundColor = Attribute.DefaultColor;
                if (Attribute.Color != null && !_colorResolver.HasError)
                {
                    backgroundColor = _colorResolver.GetValue();
                    
                }
                GUI.backgroundColor = backgroundColor;
            }
            
            string headerLabel = null;
            if (Attribute.ShowLabel)
            {
                headerLabel = _labelResolver.GetValue();
                if (string.IsNullOrEmpty(headerLabel))
                {
                    headerLabel = "Null";
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            if (Attribute.Alignment != ContentAlignment.Left) GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();

            SirenixEditorGUI.BeginBox(headerLabel, Attribute.CenterLabel);
            GUI.backgroundColor = previousBgColor;
            foreach (var child in Property.Children)
            {
                child.Draw(child.Label);
            }
            SirenixEditorGUI.EndBox();

            EditorGUILayout.EndVertical();
            if (Attribute.Alignment == ContentAlignment.Center) GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Attribute.SpaceAfter);
        }
    }
}
