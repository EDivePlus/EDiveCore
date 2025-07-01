using System;
using EDIVE.StateHandling.ToggleStates;
using Newtonsoft.Json;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class ToggleStateStatePreset : AStateValuePreset<AToggleState, bool>
    {
        public override string Title => "State";
        public override void ApplyTo(AToggleState targetObject) => targetObject.SetState(Value);
        public override void CaptureFrom(AToggleState targetObject) => Value = targetObject.State;
    }
}
