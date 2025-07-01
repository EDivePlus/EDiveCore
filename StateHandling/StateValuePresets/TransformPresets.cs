using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TransformRotationPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Rotation";
        public override void ApplyTo(Transform targetObject) => targetObject.rotation = Quaternion.Euler(Value);
        public override void CaptureFrom(Transform targetObject) => Value = targetObject.rotation.eulerAngles;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TransformLocalRotationPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Local Rotation";
        public override void ApplyTo(Transform targetObject) => targetObject.localRotation = Quaternion.Euler(Value);
        public override void CaptureFrom(Transform targetObject) => Value = targetObject.localRotation.eulerAngles;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TransformPositionPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Position";
        public override void ApplyTo(Transform targetObject) => targetObject.position = Value;
        public override void CaptureFrom(Transform targetObject) => Value = targetObject.position;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TransformLocalPositionPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Local Position";
        public override void ApplyTo(Transform targetObject) => targetObject.localPosition = Value;
        public override void CaptureFrom(Transform targetObject) => Value = targetObject.localPosition;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TransformScalePreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Scale";
        public override void ApplyTo(Transform targetObject) => targetObject.localScale = Value;
        public override void CaptureFrom(Transform targetObject) => Value = targetObject.localScale;
    }
}
