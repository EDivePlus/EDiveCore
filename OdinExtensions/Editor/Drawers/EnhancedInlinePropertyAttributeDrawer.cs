using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 0, 0.11)]
    public class EnhancedInlinePropertyAttributeDrawer : OdinAttributeDrawer<EnhancedInlinePropertyAttribute>
    {
        private PropertySearchFilter _searchFilter;
        private string _searchFieldControlName;

        protected override void Initialize()
        {
            var searchAttr = Property.GetAttribute<SearchableAttribute>();
            if (searchAttr != null)
            {
                _searchFilter = new PropertySearchFilter(Property, searchAttr);
                _searchFieldControlName = "PropertyTreeSearchField_" + Guid.NewGuid();
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (Attribute.HideLabel)
                label = null;
            
            if (label == null)
            {
                DrawChildren();
                return;
            }

            if (Attribute.PushContentRight)
            {
                SirenixEditorGUI.BeginVerticalPropertyLayout(label); 
                DrawChildren();
                GUILayout.Space(2f);
                SirenixEditorGUI.EndVerticalPropertyLayout();
            }
            else
            {
                EditorGUILayout.LabelField(label);
                GUIHelper.PushIndentLevel(EditorGUI.indentLevel + Attribute.ContentIndent);
                DrawChildren();
                GUIHelper.PopIndentLevel();
            }
        }

        private void DrawChildren()
        {
            if (_searchFilter != null)
            {
                var rect = EditorGUILayout.GetControlRect();
                var newTerm = SirenixEditorGUI.SearchField(rect, _searchFilter.SearchTerm, false, _searchFieldControlName);

                if (newTerm != _searchFilter.SearchTerm)
                {
                    _searchFilter.SearchTerm = newTerm;
                    Property.Tree.DelayActionUntilRepaint(() =>
                    {
                        _searchFilter.UpdateSearch();
                        GUIHelper.RequestRepaint();
                    });
                }
            }

            if (_searchFilter != null && _searchFilter.HasSearchResults)
            {
                _searchFilter.DrawSearchResults();
            }
            else
            {
                for (var i = 0; i < Property.Children.Count; i++)
                {
                    var child = Property.Children[i];
                    child.Draw(child.Label);
                }
            }
        }
        
    }
}
