using System;
using Newtonsoft.Json;
using TMPro;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class TMPTextTextPreset : AStateValuePreset<TMP_Text, string>
    {
        public override string Title => "Text";
        public override void ApplyTo(TMP_Text targetObject) => targetObject.text = Value;
        public override void CaptureFrom(TMP_Text targetObject) => Value = targetObject.text;
    }
}
