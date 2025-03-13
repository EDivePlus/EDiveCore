using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class GameObjectActivePreset : AStateValuePreset<GameObject, bool>
    {
        public override string Title => "Active";
        public override void ApplyTo(GameObject targetObject) => targetObject.SetActive(Value);
    }
}
