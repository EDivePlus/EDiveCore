using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TrailRendererEmittingPreset : AStateValuePreset<TrailRenderer, bool>
    {
        public override string Title => "Emitting";
        public override void ApplyTo(TrailRenderer targetObject) => targetObject.emitting = Value;
        public override void CaptureFrom(TrailRenderer targetObject) => Value = targetObject.emitting;
    }
}
