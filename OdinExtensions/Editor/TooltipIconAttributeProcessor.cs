using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public class TooltipIconAttributeProcessor : OdinAttributeProcessor
    {
        private const char TOOLTIP_ICON = '\u24d8';

        public override bool CanProcessSelfAttributes(InspectorProperty property)
        {
            return property.Attributes.HasAttribute<TooltipAttribute>() || property.Attributes.HasAttribute<PropertyTooltipAttribute>();
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            var labelText = property.GetAttribute<LabelTextAttribute>();
            var iconLabelText = property.GetAttribute<IconLabelTextAttribute>();

            if (labelText != null)
            {
                labelText.Text = AddIcon(labelText.Text);
            }
            else if (iconLabelText != null)
            {
                iconLabelText.Text = AddIcon(iconLabelText.Text);
            }
            else
            {
                var fieldName = AddIcon(property.NiceName);
                labelText = new LabelTextAttribute(fieldName);
                attributes.Add(labelText);
            }
        }

        private static string AddIcon(string text)
        {
            var suffix = $"{TOOLTIP_ICON}";
            if (text.StartsWith("@")) suffix = $"+ \" {TOOLTIP_ICON}\"";

            text = text.EndsWith(suffix) ? text : $"{text} {suffix}";
            if (text.StartsWith("$")) text = $"@{text}";
            return text;
        }
    }
}
