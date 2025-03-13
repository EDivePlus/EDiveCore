using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class TransformRotationPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Rotation";
        public override void ApplyTo(Transform targetObject) => targetObject.rotation = Quaternion.Euler(Value);
    }
    
    [Serializable, Preserve] 
    public class TransformLocalRotationPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Local Rotation";
        public override void ApplyTo(Transform targetObject) => targetObject.localRotation = Quaternion.Euler(Value);
    }
    
    [Serializable, Preserve] 
    public class TransformPositionPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Position";
        public override void ApplyTo(Transform targetObject) => targetObject.position = Value;
    }
    
    [Serializable, Preserve] 
    public class TransformLocalPositionPreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Local Position";
        public override void ApplyTo(Transform targetObject) => targetObject.localPosition = Value;
    }
    
    [Serializable, Preserve] 
    public class TransformScalePreset : AStateValuePreset<Transform, Vector3>
    {
        public override string Title => "Scale";
        public override void ApplyTo(Transform targetObject) => targetObject.localScale = Value;
    }
}
