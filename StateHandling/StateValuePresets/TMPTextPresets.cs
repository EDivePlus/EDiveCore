using System;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class TMPTextTextPreset : AStateValuePreset<TMP_Text, string>
    {
        public override string Title => "Text";
        public override void ApplyTo(TMP_Text targetObject) => targetObject.text = Value;
    }
}
