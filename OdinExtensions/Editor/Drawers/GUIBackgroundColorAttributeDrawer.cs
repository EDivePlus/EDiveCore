// Author: František Holubec
// Created: 20.10.2025

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0.5)]
    public sealed class GUIBackgroundColorAttributeDrawer : OdinAttributeDrawer<GUIBackgroundColorAttribute>
    {
        private ValueResolver<Color> _colorResolver;

        protected override void Initialize()
        {
            _colorResolver = ValueResolver.Get(Property, Attribute.GetColor, Attribute.Color);
        }
        
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_colorResolver);
            if (_colorResolver.HasError)
            {
                CallNextDrawer(label);
                return;
            }

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = _colorResolver.GetValue();
            CallNextDrawer(label);
            GUI.backgroundColor = previousColor;
        }
    }
}
