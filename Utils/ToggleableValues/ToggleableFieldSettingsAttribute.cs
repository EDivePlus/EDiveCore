// Author: František Holubec
// Created: 21.03.2025

using System;

namespace EDIVE.Utils.ToggleableValues
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ToggleableFieldSettingsAttribute : Attribute
    {
        public ToggleableFieldMode Mode { get; }

        public ToggleableFieldSettingsAttribute(ToggleableFieldMode mode)
        {
            Mode = mode;
        }
    }
}
