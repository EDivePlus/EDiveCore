using System.Linq;
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
            
            var titleContent = Attribute.HideGroupTitle
                ? GUIContent.none
                : GUIHelper.TempContent(headerLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (Attribute.Alignment != ContentAlignment.Left) GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            
            var style = new GUIStyle(SirenixGUIStyles.Label);
            if (Attribute.Bold)
            {
                style.fontStyle = FontStyle.Bold;
            }
            
            SirenixEditorGUI.BeginBox();
            GUI.backgroundColor = previousBgColor;
            
            SirenixEditorGUI.BeginBoxHeader();
            
            var hasHeaderChildren = Property.Children.Any(child => child.GetAttribute<ShowInGroupHeaderAttribute>() != null);
            if (hasHeaderChildren)
            {
                EditorGUILayout.BeginHorizontal();
                
                var titleWidth = Attribute.TitleWidth > 0 ? Attribute.TitleWidth
                    : Attribute.HideGroupTitle ? 15
                    : EditorGUIUtility.labelWidth;
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(titleWidth));
                DrawLabel(titleContent, style);
                EditorGUILayout.EndHorizontal();

                foreach (var child in Property.Children)
                {
                    if (child.GetAttribute<ShowInGroupHeaderAttribute>() == null)
                        continue;

                    GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                    GUILayout.FlexibleSpace();
                    child.Draw(child.Label);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawLabel(titleContent, style);
            }
            
            SirenixEditorGUI.EndBoxHeader();

            foreach (var child in Property.Children)
            {
                if (child.GetAttribute<ShowInGroupHeaderAttribute>() != null)
                    continue;
                child.Draw(child.Label);
            }
            SirenixEditorGUI.EndBox();

            EditorGUILayout.EndVertical();
            if (Attribute.Alignment == ContentAlignment.Center) GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(Attribute.SpaceAfter);
        }

        private void DrawLabel(GUIContent label, GUIStyle style)
        {
            var fieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.fieldWidth = 10f;
            var controlRect = EditorGUILayout.GetControlRect(false);
            EditorGUIUtility.fieldWidth = fieldWidth;
            GUI.Label(controlRect, label, style);
        }
    }
}
