// Author: Michal Petr
// Created: 17.08.2026

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class ColliderEnabledPreset : AStateValuePreset<Collider, bool>
    {
        public override string Title => "Enabled";
        public override void ApplyTo(Collider targetObject) => targetObject.enabled = Value;
        public override void CaptureFrom(Collider targetObject) => Value = targetObject.enabled;
    }
}
