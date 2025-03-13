using System;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class ToggleStateStatePreset : AStateValuePreset<AToggleState, bool>
    {
        public override string Title => "State";
        public override void ApplyTo(AToggleState targetObject) => targetObject.SetState(Value);
    }
}
