// Author: František Holubec
// Created: 13.05.2025

#if UNITY_EDITOR
using EDIVE.OdinExtensions;
using EDIVE.XRTools.Controls;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[assembly: RegisterValidator(typeof(InteractionManagerAssignerValidator))]
namespace EDIVE.XRTools.Controls
{
    public class InteractionManagerAssignerValidator : RootObjectValidator<XRBaseInteractable>
    {
        protected override void Validate(ValidationResult result)
        {
            if (Object == null)
                return;

            if (Object.interactionManager != null)
                return;

            if (!Object.TryGetComponent<InteractionManagerAssigner>(out var assigner))
            {
                result.AddError("Interactable object is missing InteractableManagerAssigner component.")
                    .WithFix(() =>
                    {
                        Object.gameObject.AddComponent<InteractionManagerAssigner>();
                        Property.ForceMarkDirty();
                    });
            }
        }
    }
}
#endif
