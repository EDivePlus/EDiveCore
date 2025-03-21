// Author: František Holubec
// Created: 21.03.2025

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace EDIVE.Utils.ToggleableValues
{
    public class ToggleableFieldSettingsAttributeProcessor<T> : OdinAttributeProcessor<ToggleableField<T>>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member.Name != "_Value") return;

            var settingsAttribute = parentProperty.GetAttribute<ToggleableFieldSettingsAttribute>();
            if (settingsAttribute == null) return;
            switch (settingsAttribute.Mode)
            {
                case ToggleableFieldMode.AlwaysActive:
                    break;

                case ToggleableFieldMode.EnabledIfChecked:
                    attributes.Add(new EnableIfAttribute(nameof(ToggleableField<object>.State)));
                    break;

                case ToggleableFieldMode.DisabledIfChecked:
                    attributes.Add(new DisableIfAttribute(nameof(ToggleableField<object>.State)));
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
#endif