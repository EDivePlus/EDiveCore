using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class TrailRendererEmittingPreset : AStateValuePreset<TrailRenderer, bool>
    {
        public override string Title => "Emitting";
        public override void ApplyTo(TrailRenderer targetObject) => targetObject.emitting = Value;
    }
}
