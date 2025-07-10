// Author: František Holubec
// Created: 10.07.2025

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class RigidbodyLinearVelocityPreset : AStateValuePreset<Rigidbody, Vector3>
    {
        public override string Title => "Linear Velocity";
        public override void ApplyTo(Rigidbody targetObject) => targetObject.linearVelocity = Value;
        public override void CaptureFrom(Rigidbody targetObject) => Value = targetObject.linearVelocity;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class RigidbodyAngularVelocityPreset : AStateValuePreset<Rigidbody, Vector3>
    {
        public override string Title => "Angular Velocity";
        public override void ApplyTo(Rigidbody targetObject) => targetObject.angularVelocity = Value;
        public override void CaptureFrom(Rigidbody targetObject) => Value = targetObject.angularVelocity;
    }
}
