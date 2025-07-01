using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutGroupChildAlignmentPreset : AStateValuePreset<LayoutGroup, TextAnchor>
    {
        public override string Title => "Child Alignment";
        public override void ApplyTo(LayoutGroup targetObject) => targetObject.childAlignment = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutGroupReverseArrangementPreset : AStateValuePreset<HorizontalOrVerticalLayoutGroup, bool>
    {
        public override string Title => "Reverse Arrangement";
        public override void ApplyTo(HorizontalOrVerticalLayoutGroup targetObject) => targetObject.reverseArrangement = Value;
    }
}
