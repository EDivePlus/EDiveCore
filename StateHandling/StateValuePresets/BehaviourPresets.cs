using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class BehaviourEnabledPreset : AStateValuePreset<Behaviour, bool>
    {
        public override string Title => "Enabled";
        public override void ApplyTo(Behaviour targetObject) => targetObject.enabled = Value;
    }
}
