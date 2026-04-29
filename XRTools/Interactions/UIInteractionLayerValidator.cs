// Author: František Holubec
// Created: 20.04.2026

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using EDIVE.XRTools.Interactions;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine.XR.Interaction.Toolkit.UI;

[assembly: RegisterValidator(typeof(UIInteractionLayerValidator))]
namespace EDIVE.XRTools.Interactions
{
    public class UIInteractionLayerValidator : RootObjectValidator<UIInteractionLayer>
    {
        protected override void Validate(ValidationResult result)
        {
            if (Object == null)
                return;
            
            var raycaster = Object.GetComponentInParent<TrackedDeviceGraphicRaycaster>();
            if (raycaster is not FilteredTrackedDeviceGraphicRaycaster)
            {
                result.AddError("UI Interaction Layer must be used with FilteredTrackedDeviceGraphicRaycaster")
                    .WithFix(() =>
                    {
                        raycaster.ChangeScriptType<FilteredTrackedDeviceGraphicRaycaster>();
                    });
            }
        }
    }
}
#endif