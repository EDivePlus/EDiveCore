using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class LayoutGroupChildAlignmentPreset : AStateValuePreset<LayoutGroup, TextAnchor>
    {
        public override string Title => "Child Alignment";
        public override void ApplyTo(LayoutGroup targetObject) => targetObject.childAlignment = Value;
    }

    [Serializable, Preserve] 
    public class LayoutGroupReverseArrangementPreset : AStateValuePreset<HorizontalOrVerticalLayoutGroup, bool>
    {
        public override string Title => "Reverse Arrangement";
        public override void ApplyTo(HorizontalOrVerticalLayoutGroup targetObject) => targetObject.reverseArrangement = Value;
    }
}
