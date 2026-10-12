using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public abstract class ATooltipIconAttributeDrawer<TAttribute> : OdinAttributeDrawer<TAttribute>
        where TAttribute : Attribute
    {
        private const char TOOLTIP_ICON = 'ⓘ';

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (label != null && (label.text == null || !label.text.EndsWith($"{TOOLTIP_ICON}")))
            {
                label = new GUIContent(label) {text = $"{label.text} {TOOLTIP_ICON}"};
            }

            CallNextDrawer(label);
        }
    }

    // Odin reads the priority only from the drawer type itself. Wrapper level, so it runs before
    // attribute drawers that draw the label themselves, like IconEnumToggleButtons.
    [DrawerPriority(0, 1, 0)]
    public class TooltipIconAttributeDrawer : ATooltipIconAttributeDrawer<TooltipAttribute> { }

    [DrawerPriority(0, 1, 0)]
    public class PropertyTooltipIconAttributeDrawer : ATooltipIconAttributeDrawer<PropertyTooltipAttribute> { }
}
