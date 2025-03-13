using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class CanvasSortingLayerPreset : AStateValuePreset<Canvas>
    {
        [SortingLayer]
        [SerializeField]
        private string _SortingLayer = "Default";
        
        public override string Title => "Sorting Layer";
        public override void ApplyTo(Canvas targetObject) => targetObject.sortingLayerName = _SortingLayer;
    }

    [Serializable, Preserve] 
    public class CanvasSortingOrderPreset : AStateValuePreset<Canvas, int>
    {
        public override string Title => "Sorting Order";
        public override void ApplyTo(Canvas targetObject) => targetObject.sortingOrder = Value;
    }
}
