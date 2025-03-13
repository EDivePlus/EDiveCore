using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class SortingLayerAttributeDrawer : OdinAttributeDrawer<SortingLayerAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var selected = ValueEntry.SmartValue;
            SortingLayerSelector<string>.DrawSelectorDropdown(label, selected, rect =>
            {
                var values = SortingLayer.layers.Select(l => l.name);
                var selector = new SortingLayerSelector<string>(null, false, x => x, values);
                selector.SetSelection(ValueEntry.SmartValue);
                selector.SelectionTree.Config.DrawSearchToolbar = false;
                selector.EnableSingleClickToSelect();
                selector.SelectionConfirmed += selection =>
                {
                    Property.ValueEntry.WeakSmartValue = selection.FirstOrDefault();
                    Property.MarkSerializationRootDirty();
                };
                selector.ShowInPopup(rect);
                return selector;
            });
        }

        private class SortingLayerSelector<T> : GenericSelector<T>
        {
            public SortingLayerSelector(string title, bool supportsMultiSelect, Func<T, string> getMenuItemName, IEnumerable<T> collection) 
                : base(title, supportsMultiSelect, getMenuItemName, collection) { }
            
            protected override void DrawSelectionTree()
            {
                base.DrawSelectionTree();
                if (GUILayout.Button("Add Sorting Layer ..."))
                {
                    SettingsService.OpenProjectSettings("Project/Tags and Layers");
                }
            }
        }
    }
}
