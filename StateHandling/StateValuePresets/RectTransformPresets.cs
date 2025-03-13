using System;
using EDIVE.DataStructures.RectTransformPreset;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class RectTransformAnchoredPositionPreset : AStateValuePreset<RectTransform, Vector2>
    {
        public override string Title => "Anchored Position";
        public override void ApplyTo(RectTransform targetObject) => targetObject.anchoredPosition = Value;
    }
    
    [Serializable, Preserve] 
    public class RectTransformSizeDeltaPreset : AStateValuePreset<RectTransform, Vector2>
    {
        public override string Title => "Size Delta";
        public override void ApplyTo(RectTransform targetObject) => targetObject.sizeDelta = Value;
    }

    [Serializable, Preserve] 
    public class RectTransformPivotPreset : AStateValuePreset<RectTransform, Vector2>
    {
        public override string Title => "Pivot";
        public override void ApplyTo(RectTransform targetObject) => targetObject.pivot = Value;
    }
    
    [Serializable, Preserve] 
    public class RectTransformSnapshotPreset : AStateValuePreset<RectTransform, RectTransformSnapshot>
    {
        public override string Title => "Layout";
        public override void ApplyTo(RectTransform targetObject) => Value.ApplyTo(targetObject);
    }
}
