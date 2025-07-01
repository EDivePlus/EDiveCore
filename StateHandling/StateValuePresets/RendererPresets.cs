using System;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RendererEnabledPreset : AStateValuePreset<Renderer, bool>
    {
        public override string Title => "Enabled";
        public override void ApplyTo(Renderer targetObject) => targetObject.enabled = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class RendererMaterialPreset : AStateValuePreset<Renderer, Material>
    {
        [MinValue(0)]
        [SerializeField]
        [JsonProperty("MaterialIndex")]
        private int _MaterialIndex;

        [SerializeField]
        [JsonProperty("UseSharedMaterial")]
        private bool _UseSharedMaterial;

        public override string Title => "Material";

        public override void ApplyTo(Renderer targetObject)
        {
            var materials = _UseSharedMaterial ? targetObject.sharedMaterials : targetObject.materials;
            if (_MaterialIndex >= materials.Length)
                return;
            materials[_MaterialIndex] = Value;

            if (_UseSharedMaterial)
                targetObject.sharedMaterials = materials;
            else
                targetObject.materials = materials;
        }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RendererSortingLayerPreset : AStateValuePreset<Renderer, string>
    {
        public override string Title => "Sorting Layer";
        public override void ApplyTo(Renderer targetObject) => targetObject.sortingLayerName = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class RendererSortingOrderPreset : AStateValuePreset<Renderer, int>
    {
        public override string Title => "Sorting Order";
        public override void ApplyTo(Renderer targetObject) => targetObject.sortingOrder = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public abstract class ARendererMaterialPreset<TRenderer, TValue> : AStateValuePreset<TRenderer, TValue> where TRenderer : Renderer
    {
        [MinValue(0)]
        [SerializeField]
        [JsonProperty("MaterialIndex")]
        private int _MaterialIndex;
        
        [SerializeField]
        [JsonProperty("UseSharedMaterial")]
        private bool _UseSharedMaterial;
        
        private bool TryGetMaterial(TRenderer target, out Material material)
        {
            var materials = _UseSharedMaterial ? target.sharedMaterials : target.materials;
            material = null;

            if (materials.Length == 0 || _MaterialIndex >= materials.Length)
                return false;

            material = materials[_MaterialIndex];
            return true;
        }

        public override void ApplyTo(TRenderer targetObject)
        {
            if (TryGetMaterial(targetObject, out var material))
                ApplyTo(material);
        }

        public abstract void ApplyTo(Material targetMaterial);
    }

    [Preserve]
    public class RendererMaterialAlphaPreset : ARendererMaterialPreset<Renderer, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(Material targetObject) => targetObject.color = targetObject.color.WithA(Value);
    }

    [Preserve]
    public class RendererMaterialColorPreset : ARendererMaterialPreset<Renderer, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(Material targetObject) => targetObject.color = Value;
    }
}
