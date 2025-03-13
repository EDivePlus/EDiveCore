using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class RendererEnabledPreset : AStateValuePreset<Renderer, bool>
    {
        public override string Title => "Enabled";
        public override void ApplyTo(Renderer targetObject) => targetObject.enabled = Value;
    }
    
    [Serializable, Preserve] 
    public class RendererMaterialPreset : AStateValuePreset<Renderer, Material>
    {
        public override string Title => "Material";
        public override void ApplyTo(Renderer targetObject) => targetObject.material = Value;
    }
    
    [Serializable, Preserve] 
    public class RendererSortingLayerPreset : AStateValuePreset<Renderer, string>
    {
        public override string Title => "Sorting Layer";
        public override void ApplyTo(Renderer targetObject) => targetObject.sortingLayerName = Value;
    }
    
    [Serializable, Preserve] 
    public class RendererSortingOrderPreset : AStateValuePreset<Renderer, int>
    {
        public override string Title => "Sorting Order";
        public override void ApplyTo(Renderer targetObject) => targetObject.sortingOrder = Value;
    }
}
