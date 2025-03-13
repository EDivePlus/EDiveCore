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

        /// <summary>Initializes this instance.</summary>
        protected override void Initialize()
        {
            _labelResolver = ValueResolver.GetForString(Property, Attribute.LabelText ?? Attribute.GroupName);
            _colorResolver = ValueResolver.Get(Property, Attribute.Color, Attribute.DefaultColor);
            _useIfResolver = ValueResolver.Get(Property, Attribute.UseIf, true);
        }

        /// <summary>Draws the property.</summary>
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
            
            var headerLabel = (string) null;
            if (Attribute.ShowLabel)
            {
                headerLabel = _labelResolver.GetValue();
                if (string.IsNullOrEmpty(headerLabel))
                {
                    headerLabel = "Null"; // The user has asked for a header. So he gets a header.
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
