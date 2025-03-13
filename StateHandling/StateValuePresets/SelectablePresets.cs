using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class SelectableInteractablePreset : AStateValuePreset<Selectable, bool>
    {
        public override string Title => "Interactable";
        public override void ApplyTo(Selectable targetObject) => targetObject.interactable = Value;
    }
}
