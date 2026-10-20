// Author: František Holubec
// Created: 19.10.2026

using System;
using EDIVE.StateHandling.StateValuePresets;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling.Animation
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class ObjectScalerSizePreset : AStateValuePreset<ObjectScaler, Vector3>
    {
        public override string Title => "Size";
        public override void ApplyTo(ObjectScaler targetObject) => targetObject.Size = Value;
        public override void CaptureFrom(ObjectScaler targetObject) => Value = targetObject.Size;
    }
}
