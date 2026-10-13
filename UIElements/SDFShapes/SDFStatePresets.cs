// Author: František Holubec
// Created: 12.10.2026

using System;
using EDIVE.DataStructures.VariableFields;
using EDIVE.StateHandling.StateValuePresets;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.UIElements.SDFShapes
{
    [Serializable, Preserve]
    public class SDFGraphicRoundnessPreset : AStateValuePreset<SDFGraphic, CornerRoundness>
    {
        public override string Title => "Roundness";
        public override void ApplyTo(SDFGraphic targetObject) => targetObject.Roundness = Value;
        public override void CaptureFrom(SDFGraphic targetObject) => Value = targetObject.Roundness;
    }

    [Serializable, Preserve]
    public class SDFOutlineColorPreset : AStateValuePreset<SDFOutline, VariableField<Color>>
    {
        public SDFOutlineColorPreset() : base(new VariableField<Color>(Color.black)) { }

        public override string Title => "Outline Color";
        public override void ApplyTo(SDFOutline targetObject) => targetObject.Color = Value.Value;
        public override void CaptureFrom(SDFOutline targetObject) => Value.Value = targetObject.Color;
    }

    [Serializable, Preserve]
    public class SDFOutlineSizePreset : AStateValuePreset<SDFOutline, float>
    {
        public override string Title => "Outline Size";
        public override void ApplyTo(SDFOutline targetObject) => targetObject.Size = Value;
        public override void CaptureFrom(SDFOutline targetObject) => Value = targetObject.Size;
    }

    [Serializable, Preserve]
    public class SDFShadowColorPreset : AStateValuePreset<SDFShadow, VariableField<Color>>
    {
        public SDFShadowColorPreset() : base(new VariableField<Color>(Color.black)) { }

        public override string Title => "Shadow Color";
        public override void ApplyTo(SDFShadow targetObject) => targetObject.Color = Value.Value;
        public override void CaptureFrom(SDFShadow targetObject) => Value.Value = targetObject.Color;
    }

    [Serializable, Preserve]
    public class SDFShadowSizePreset : AStateValuePreset<SDFShadow, float>
    {
        public override string Title => "Shadow Size";
        public override void ApplyTo(SDFShadow targetObject) => targetObject.Size = Value;
        public override void CaptureFrom(SDFShadow targetObject) => Value = targetObject.Size;
    }

    [Serializable, Preserve]
    public class SDFShadowBlurPreset : AStateValuePreset<SDFShadow, float>
    {
        public override string Title => "Shadow Blur";
        public override void ApplyTo(SDFShadow targetObject) => targetObject.Blur = Value;
        public override void CaptureFrom(SDFShadow targetObject) => Value = targetObject.Blur;
    }

    [Serializable, Preserve]
    public class SDFArcValuePreset : AStateValuePreset<SDFArc, float>
    {
        public override string Title => "Arc Value";
        public override void ApplyTo(SDFArc targetObject) => targetObject.Value = Value;
        public override void CaptureFrom(SDFArc targetObject) => Value = targetObject.Value;
    }
}
