using System;
using Newtonsoft.Json;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementMinHeightPreset : AStateValuePreset<LayoutElement, float>
    {
        public override string Title => "Min Height";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.minHeight = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementMinWidthPreset : AStateValuePreset<LayoutElement, float>
    {
        public override string Title => "Min Width";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.minWidth = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementPreferredHeightPreset : AStateValuePreset<LayoutElement, float>
    {
        public override string Title => "Preferred Height";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.preferredHeight = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementPreferredWidthPreset : AStateValuePreset<LayoutElement, float>
    {
        public override string Title => "Preferred  Width";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.preferredWidth = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementFlexibleHeightPreset : AStateValuePreset<LayoutElement, float>
    {
        public override string Title => "Flexible Height";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.flexibleHeight = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementFlexibleWidthPreset : AStateValuePreset<LayoutElement, float>
    {
        public override string Title => "Flexible Width";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.flexibleWidth = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementIgnorePreset : AStateValuePreset<LayoutElement, bool>
    {
        public override string Title => "Ignore Layout";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.ignoreLayout = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LayoutElementPriorityPreset : AStateValuePreset<LayoutElement, int>
    {
        public override string Title => "Layout Priority";
        public override void ApplyTo(LayoutElement targetObject) => targetObject.layoutPriority = Value;
    }
}
