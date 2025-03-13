using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class IconLabelTextAttributeDrawer : OdinAttributeDrawer<IconLabelTextAttribute>
    {
        private ValueResolver<string> _textProvider;
        private ValueResolver<string> _iconNameResolver;
        private GUIContent _overrideLabel;

        protected override void Initialize()
        {
            _textProvider = ValueResolver.GetForString(this.Property, this.Attribute.Text);
            _iconNameResolver = ValueResolver.GetForString(Property, Attribute.IconName);
            _overrideLabel = new GUIContent();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_textProvider, _iconNameResolver);
            if (_textProvider.HasError || _iconNameResolver.HasError)
            {
                CallNextDrawer(label);
                return;
            }

            var icon = EditorIconsUtility.GetIcon(_iconNameResolver.GetValue(), Attribute.Bundle);
            var str = _textProvider.GetValue();

            if (str == null && icon == null)
            {
                CallNextDrawer(label);
                return;
            }

            var lbl = Attribute.HideText ? " " : str ?? label.text;
            if (Attribute.NicifyText)
                lbl = ObjectNames.NicifyVariableName(lbl);

            _overrideLabel.text = lbl;

            if (icon != null)
                _overrideLabel.image = icon.GetTexture(Attribute.Type);

            CallNextDrawer(_overrideLabel);
        }
    }
}
