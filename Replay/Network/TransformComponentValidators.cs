// Author: František Holubec
// Created: 25.02.2026

using EDIVE.OdinExtensions;
using EDIVE.Replay.Components;
using EDIVE.Replay.Network;
using FishNet.Component.Transforming;
using FishNet.Object;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(TransformPositionRotationComponentNetValidator))]
[assembly: RegisterValidator(typeof(TransformScaleComponentNetValidator))]
namespace EDIVE.Replay.Network
{
    public class TransformPositionRotationComponentNetValidator : ValueValidator<TransformPositionRotationComponent>
    {
        protected override void Validate(ValidationResult result)
        {
            if (Value == null || Value.Target == null)
                return;

            // Check if networked
            if (Value.Target.GetComponentInParent<NetworkObject>() == null)
                return;
            
            if (!Value.Target.TryGetComponent<NetworkTransform>(out _))
            {
                result.AddError("Recorded target is networked and does not have NetworkTransform component.")
                    .WithFix(() =>
                    {
                        Value.Target.gameObject.AddComponent<NetworkTransform>();
                        Property.ForceMarkDirty();
                    });
            }
        }
    }
    
    public class TransformScaleComponentNetValidator : ValueValidator<TransformScaleComponent>
    {
        protected override void Validate(ValidationResult result)
        {
            if (Value == null || Value.Target == null)
                return;

            // Check if networked
            if (Value.Target.GetComponentInParent<NetworkObject>() == null)
                return;
            
            if (!Value.Target.TryGetComponent<NetworkTransform>(out _))
            {
                result.AddError("Recorded target is networked and does not have NetworkTransform component.")
                    .WithFix(() =>
                    {
                        Value.Target.gameObject.AddComponent<NetworkTransform>();
                        Property.ForceMarkDirty();
                    });
            }
        }
    }
}
