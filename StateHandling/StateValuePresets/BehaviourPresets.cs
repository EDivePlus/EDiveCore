using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{ 
    [Serializable, Preserve] 
    public class BehaviourEnabledPreset : AStateValuePreset<Behaviour, bool>
    {
        public override string Title => "Enabled";
        public override void ApplyTo(Behaviour targetObject) => targetObject.enabled = Value;
    }
}
