// Author: František Holubec
// Created: 16.03.2025

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class InlineListAttributeDrawer
    {
        public class SimpleListAttributeDrawer : OdinAttributeDrawer<InlineListAttribute>
        {
            private const string INDEX_ARGUMENT_ID = "index";
            private ValueResolver<string> _elementSuffixResolver;

            protected override void Initialize()
            {
                base.Initialize();
                _elementSuffixResolver = ValueResolver.GetForString(Property, Attribute.ElementSuffixGetter, new NamedValue(INDEX_ARGUMENT_ID, typeof(int)));
            }

            protected override bool CanDrawAttributeProperty(InspectorProperty property)
            {
                return property.ChildResolver is IOrderedCollectionResolver;
            }

            protected override void DrawPropertyLayout(GUIContent label)
            {
                ValueResolver.DrawErrors(_elementSuffixResolver);
                SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
                for (var index = 0; index < Property.Children.Count; index++)
                {
                    var child = Property.Children[index];
                    child.Draw(null);
                    if (Attribute.ElementSuffixGetter != null && !_elementSuffixResolver.HasError)
                    {
                        GUIHelper.PushGUIEnabled(true);
                        _elementSuffixResolver.Context.NamedValues.Set(INDEX_ARGUMENT_ID, index);
                        var suffix = _elementSuffixResolver.GetValue();
                        GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(0.0f, 8f), suffix, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                        GUIHelper.PopGUIEnabled();
                    }
                }

                SirenixEditorGUI.EndHorizontalPropertyLayout();
            }
        }
    }
}
