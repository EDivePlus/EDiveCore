using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 0, 1)]
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

    public class TooltipIconAttributeDrawer : ATooltipIconAttributeDrawer<TooltipAttribute> { }

    public class PropertyTooltipIconAttributeDrawer : ATooltipIconAttributeDrawer<PropertyTooltipAttribute> { }
}
