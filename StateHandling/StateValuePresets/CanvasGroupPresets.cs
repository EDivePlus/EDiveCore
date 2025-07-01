using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasGroupInteractablePreset : AStateValuePreset<CanvasGroup, bool>
    {
        public override string Title => "Interactable";
        public override void ApplyTo(CanvasGroup targetObject) => targetObject.interactable = Value;
        public override void CaptureFrom(CanvasGroup targetObject) => Value = targetObject.interactable;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasGroupBlockRaycastPreset : AStateValuePreset<CanvasGroup, bool>
    {
        public override string Title => "Block Raycasts";
        public override void ApplyTo(CanvasGroup targetObject) => targetObject.blocksRaycasts = Value;
        public override void CaptureFrom(CanvasGroup targetObject) => Value = targetObject.blocksRaycasts;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasGroupAlphaPreset : AStateValuePreset<CanvasGroup, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(CanvasGroup targetObject) => targetObject.alpha = Value;
        public override void CaptureFrom(CanvasGroup targetObject) => Value = targetObject.alpha;
    }
}
