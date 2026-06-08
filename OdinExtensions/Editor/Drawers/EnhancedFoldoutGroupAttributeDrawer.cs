using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class EnhancedFoldoutGroupAttributeDrawer : OdinGroupDrawer<EnhancedFoldoutGroupAttribute>
    {
        private ValueResolver<Color> _colorResolver;
        private ValueResolver<string> _titleResolver;
        private ValueResolver<bool> _useIfResolver;
        
        protected override void Initialize()
        {
            _colorResolver = ValueResolver.Get(Property, Attribute.Color, Attribute.DefaultColor);
            _titleResolver = ValueResolver.Get(Property, Attribute.GroupName, Attribute.GroupName);
            _useIfResolver = ValueResolver.Get(Property, Attribute.UseIf, true);

            if (Attribute.HasDefinedExpanded)
                Property.State.Expanded = Attribute.Expanded;
        }
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_useIfResolver);
            if (!_useIfResolver.GetValue())
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

            var titleContent = Attribute.HideGroupTitle
                ? GUIContent.none
                : GUIHelper.TempContent(_titleResolver.GetValue());

            var hasHeaderChildren = Property.Children.Any(child => child.GetAttribute<ShowInFoldoutHeaderAttribute>() != null);
            if (hasHeaderChildren)
            {
                EditorGUILayout.BeginHorizontal();
                
                var titleWidth = Attribute.TitleWidth > 0 ? Attribute.TitleWidth
                    : Attribute.HideGroupTitle ? 15
                    : EditorGUIUtility.labelWidth;
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(titleWidth));
                Property.State.Expanded = SirenixEditorGUI.Foldout(Property.State.Expanded, titleContent, style);
                EditorGUILayout.EndHorizontal();

                foreach (var child in Property.Children)
                {
                    if (child.GetAttribute<ShowInFoldoutHeaderAttribute>() != null)
                        child.Draw(child.Label);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                Property.State.Expanded = SirenixEditorGUI.Foldout(Property.State.Expanded, titleContent, style);
            }

            SirenixEditorGUI.EndBoxHeader();
            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded))
            {
                foreach (var child in Property.Children)
                {
                    if (child.GetAttribute<ShowInFoldoutHeaderAttribute>() != null)
                        continue;
                    child.Draw(child.Label);
                }
            }
            SirenixEditorGUI.EndFadeGroup();
            SirenixEditorGUI.EndBox();
            GUILayout.Space(Attribute.SpaceAfter);
        }
    }
}
