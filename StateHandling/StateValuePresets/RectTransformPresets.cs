using System;
using EDIVE.DataStructures.RectTransformSnapshot;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RectTransformAnchoredPositionPreset : AStateValuePreset<RectTransform, Vector2>
    {
        public override string Title => "Anchored Position";
        public override void ApplyTo(RectTransform targetObject) => targetObject.anchoredPosition = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RectTransformSizeDeltaPreset : AStateValuePreset<RectTransform, Vector2>
    {
        public override string Title => "Size Delta";
        public override void ApplyTo(RectTransform targetObject) => targetObject.sizeDelta = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RectTransformPivotPreset : AStateValuePreset<RectTransform, Vector2>
    {
        public override string Title => "Pivot";
        public override void ApplyTo(RectTransform targetObject) => targetObject.pivot = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RectTransformSnapshotPreset : AStateValuePreset<RectTransform, RectTransformSnapshot>
    {
        public override string Title => "Layout";
        public override void ApplyTo(RectTransform targetObject) => Value.ApplyTo(targetObject);
    }
}
