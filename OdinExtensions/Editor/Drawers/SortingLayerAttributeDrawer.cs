using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class IntSortingLayerAttributeDrawer : OdinAttributeDrawer<SortingLayerAttribute, int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var selected = SortingLayer.IDToName(ValueEntry.SmartValue);
            StringSortingLayerAttributeDrawer.SortingLayerDrawerUtility.DrawSortingLayerField(label, selected, selection =>
            {
                Property.ValueEntry.WeakSmartValue = SortingLayer.NameToID(selection);
                Property.MarkSerializationRootDirty();
            });
        }
    }

    public class StringSortingLayerAttributeDrawer : OdinAttributeDrawer<SortingLayerAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            SortingLayerDrawerUtility.DrawSortingLayerField(label, ValueEntry.SmartValue, selection =>
            {
                Property.ValueEntry.WeakSmartValue = selection;
                Property.MarkSerializationRootDirty();
            });
        }

        public static class SortingLayerDrawerUtility
        {
            public static void DrawSortingLayerField(GUIContent label, string selected, Action<string> onSelectionChanged)
            {
                SortingLayerSelector<string>.DrawSelectorDropdown(label, selected, rect =>
                {
                    var values = SortingLayer.layers.Select(l => l.name);
                    var selector = new SortingLayerSelector<string>(null, false, x => x, values);
                    selector.SetSelection(selected);
                    selector.SelectionTree.Config.DrawSearchToolbar = false;
                    selector.EnableSingleClickToSelect();
                    selector.SelectionConfirmed += selection => { onSelectionChanged?.Invoke(selection.FirstOrDefault()); };
                    selector.ShowInPopup(rect);
                    return selector;
                });
            }

            private class SortingLayerSelector<T> : GenericSelector<T>
            {
                public SortingLayerSelector(string title, bool supportsMultiSelect, Func<T, string> getMenuItemName, IEnumerable<T> collection)
                    : base(title, supportsMultiSelect, getMenuItemName, collection)
                {
                }

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
}
