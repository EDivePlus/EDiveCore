using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public sealed class IconButtonAttributeDrawer : OdinAttributeDrawer<IconButtonAttribute>
    {
        private ValueResolver<string> _iconNameResolver;
        private ValueResolver<string> _tooltipResolver;
        private ValueResolver<string> _labelResolver;
        private ActionResolver _clickAction;

        protected override void Initialize()
        {
            _iconNameResolver = ValueResolver.GetForString(Property, Attribute.IconName);
            _tooltipResolver = ValueResolver.GetForString(Property, Attribute.Tooltip);
            _labelResolver = ValueResolver.GetForString(Property, Attribute.Label);
            _clickAction = ActionResolver.Get(Property, null);
        }

        /// <summary>
        /// Draws the property.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_iconNameResolver, _tooltipResolver, _labelResolver);
            ActionResolver.DrawErrors(_clickAction);
            if (_iconNameResolver.HasError || _clickAction.HasError )
            {
                CallNextDrawer(label);
                return;
            }
            
            var icon = EditorIconsUtility.GetIcon(_iconNameResolver.GetValue(), Attribute.Bundle);
            if (icon == null)
            {
                CallNextDrawer(label);
                return;
            }
            
            var size = Attribute.HasDefinedSize ? Attribute.Size : 18;
            var tooltip = !_tooltipResolver.HasError ? _tooltipResolver.GetValue() : "";
            if (Attribute.DrawAsButton)
            {
                string text = null;
                if (Attribute.ShowLabel)
                {
                    text = !_labelResolver.HasError ? _labelResolver.GetValue() : label.text;
                }

                var content = GUIHelper.TempContent($" {text}", icon.Highlighted, tooltip);
                var btnRect = GUILayoutUtility.GetRect(0, size);
                if (GUI.Button(btnRect, content))
                {
                    Property.RecordForUndo($"Click {Property.Name}");
                    _clickAction.DoActionForAllSelectionIndices();
                }
            }
            else
            {
                var rect = GUILayoutUtility.GetRect(size, size, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(size));
                if (SirenixEditorGUI.IconButton(rect, icon,tooltip))
                {
                    Property.RecordForUndo($"Click {Property.Name}");
                    _clickAction.DoActionForAllSelectionIndices();
                }
            }
        }
    }
}
