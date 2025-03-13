using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class EnhancedFoldoutGroupAttributeDrawer : OdinGroupDrawer<EnhancedFoldoutGroupAttribute>
    {
        private ValueResolver<Color> _colorResolver;
        private ValueResolver<string> _titleResolver;
        private ValueResolver<bool> _useIfResolver;

        /// <summary>Initializes this instance.</summary>
        protected override void Initialize()
        {
            _colorResolver = ValueResolver.Get(Property, Attribute.Color, Attribute.DefaultColor);
            _titleResolver = ValueResolver.Get(Property, Attribute.GroupName, Attribute.GroupName);
            _useIfResolver = ValueResolver.Get(Property, Attribute.UseIf, true);

            if (Attribute.HasDefinedExpanded)
                Property.State.Expanded = Attribute.Expanded;
        }

        /// <summary>Draws the property.</summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_useIfResolver);
            if (_useIfResolver.GetValue() == false)
            {
                foreach (var child in Property.Children)
                {
                    child.Draw(child.Label);
                }
                return;
            }

            GUILayout.Space(Attribute.SpaceBefore);
            ValueResolver.DrawErrors(_colorResolver, _titleResolver);

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
            
            SirenixEditorGUI.BeginBox();
            GUI.backgroundColor = previousBgColor;
            SirenixEditorGUI.BeginBoxHeader();
            var style = new GUIStyle(SirenixGUIStyles.Foldout);
            if (Attribute.Bold)
            {
                style.fontStyle = FontStyle.Bold;
            }
            Property.State.Expanded = SirenixEditorGUI.Foldout(Property.State.Expanded, GUIHelper.TempContent(_titleResolver.GetValue()), style);
            SirenixEditorGUI.EndBoxHeader();
            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded))
            {
                foreach (var child in Property.Children)
                {
                    child.Draw(child.Label);
                }
            }
            SirenixEditorGUI.EndFadeGroup();
            SirenixEditorGUI.EndBox();
            GUILayout.Space(Attribute.SpaceAfter);
        }
    }
}
