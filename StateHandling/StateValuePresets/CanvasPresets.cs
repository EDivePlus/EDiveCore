using System;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasSortingLayerPreset : AStateValuePreset<Canvas>
    {
        [SortingLayer]
        [SerializeField]
        [JsonProperty("SortingLayer")]
        private string _SortingLayer = "Default";
        
        public override string Title => "Sorting Layer";
        public override void ApplyTo(Canvas targetObject) => targetObject.sortingLayerName = _SortingLayer;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class CanvasSortingOrderPreset : AStateValuePreset<Canvas, int>
    {
        public override string Title => "Sorting Order";
        public override void ApplyTo(Canvas targetObject) => targetObject.sortingOrder = Value;
    }
}
