using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class CanvasGroupInteractablePreset : AStateValuePreset<CanvasGroup, bool>
    {
        public override string Title => "Interactable";
        public override void ApplyTo(CanvasGroup targetObject) => targetObject.interactable = Value;
    }
    
    [Serializable, Preserve] 
    public class CanvasGroupBlockRaycastPreset : AStateValuePreset<CanvasGroup, bool>
    {
        public override string Title => "Block Raycasts";
        public override void ApplyTo(CanvasGroup targetObject) => targetObject.blocksRaycasts = Value;
    }
    
    [Serializable, Preserve] 
    public class CanvasGroupAlphaPreset : AStateValuePreset<CanvasGroup, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(CanvasGroup targetObject) => targetObject.alpha = Value;
    }
}
