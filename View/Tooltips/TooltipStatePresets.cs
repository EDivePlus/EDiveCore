// Author: František Holubec
// Created: 14.10.2026

using System;
using EDIVE.StateHandling.StateValuePresets;
using EDIVE.VisualPresets.Presets;
using UnityEngine.Scripting;

namespace EDIVE.View.Tooltips
{
    // Tooltip content per state, e.g. a status icon
    [Serializable, Preserve]
    public class TooltipTriggerPresetPreset : AStateValuePreset<TooltipTrigger, VisualPreset>
    {
        public override string Title => "Tooltip";
        public override void ApplyTo(TooltipTrigger targetObject) => targetObject.SetPreset(Value);
        public override void CaptureFrom(TooltipTrigger targetObject) => Value = targetObject.Preset;
    }
}
