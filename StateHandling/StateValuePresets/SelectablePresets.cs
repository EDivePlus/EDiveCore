using System;
using Newtonsoft.Json;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SelectableInteractablePreset : AStateValuePreset<Selectable, bool>
    {
        public override string Title => "Interactable";
        public override void ApplyTo(Selectable targetObject) => targetObject.interactable = Value;
    }
}
