using System;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasSortingLayerPreset : AStateValuePreset<Canvas>
    {
        [FormerlySerializedAs("_SortingLayer")]
        [SortingLayer]
        [LabelText("$Title")]
        [SerializeField]
        [JsonProperty("Value")]
        private string _Value = "Default";
        
        public override string Title => "Sorting Layer";
        public override void ApplyTo(Canvas targetObject) => targetObject.sortingLayerName = _Value;
        public override void CaptureFrom(Canvas targetObject) => _Value = targetObject.sortingLayerName;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasSortingOrderPreset : AStateValuePreset<Canvas, int>
    {
        public override string Title => "Sorting Order";
        public override void ApplyTo(Canvas targetObject) => targetObject.sortingOrder = Value;
        public override void CaptureFrom(Canvas targetObject) => Value = targetObject.sortingOrder;
    }
}
