using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening.Segments;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Tweening.ObjectActions
{
    public static class RendererPropertyTweenActionUtils
    {
#if UNITY_EDITOR
        public static IEnumerable GetMaterialPropertyNames(InspectorProperty property, int materialIndex, params ShaderPropertyType[] propertyTypes)
        {
            if (!property.TryGetParentObject<ObjectTweenSegment>(out var segment) || segment.Target is not Renderer renderer)
                return null;

            var materials = renderer.sharedMaterials;
            if (materials.Length == 0)
                return null;

            var names = new List<string>();
            var seen = new HashSet<string>();
            for (var m = 0; m < materials.Length; m++)
            {
                if (materialIndex >= 0 && m != materialIndex)
                    continue;

                var material = materials[m];
                if (material == null || material.shader == null)
                    continue;

                var shader = material.shader;
                var count = shader.GetPropertyCount();
                for (var i = 0; i < count; i++)
                {
                    if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0)
                        continue;
                    if (Array.IndexOf(propertyTypes, shader.GetPropertyType(i)) < 0)
                        continue;

                    var name = shader.GetPropertyName(i);
                    if (seen.Add(name))
                        names.Add(name);
                }
            }
            return names;
        }
        
        public static void ClearPropertyBlock(InspectorProperty property, int materialIndex)
        {
            if (!property.TryGetParentObject<ObjectTweenSegment>(out var segment) || segment.Target is not Renderer renderer)
                return;
            
            if (materialIndex < 0)
                renderer.SetPropertyBlock(null);
            else
                renderer.SetPropertyBlock(null, materialIndex);
        }
#endif
    }
    
    [Serializable]
    public abstract class ARendererMaterialTweenAction<TRenderer> : ATweenObjectAction<TRenderer> where TRenderer : Renderer
    {
        [SerializeField]
        private bool _UseSharedMaterial;

        [MinValue(0)]
        [SerializeField]
        protected int _MaterialIndex;

        private bool TryGetMaterial(TRenderer target, out Material material)
        {
            var materials = Application.isPlaying || _UseSharedMaterial ? target.sharedMaterials : target.materials;
            material = null;

            if (materials.Length == 0 || _MaterialIndex >= materials.Length)
                return false;

            material = materials[_MaterialIndex];
            return true;
        }

        protected override Tween GetTween(TRenderer target)
        {
            return !TryGetMaterial(target, out var material) ? null : GetTween(material);
        }

        protected abstract Tween GetTween(Material material);

#if UNITY_EDITOR
        protected void ClearPropertyBlock(InspectorProperty property) => RendererPropertyTweenActionUtils.ClearPropertyBlock(property, _MaterialIndex);
#endif
    }
    
    [Serializable]
    public abstract class ARendererPropertyBlockTweenAction<TRenderer> : ATweenObjectAction<TRenderer> where TRenderer : Renderer
    {
        [MinValue(-1)]
        [Tooltip("Material index to target. Use -1 to apply to the whole renderer (all materials).")]
        [SerializeField]
        protected int _MaterialIndex = -1;

        [SerializeField]
        [EnhancedValueDropdown("GetMaterialPropertyNames", AppendNextDrawer = true)]
        [InlineIconButton(FontAwesomeEditorIconType.BroomSolid, "ClearPropertyBlock", "Clear property block")]
        protected string _Property;

        protected void GetBlock(Renderer renderer, MaterialPropertyBlock block)
        {
            if (_MaterialIndex < 0)
                renderer.GetPropertyBlock(block);
            else
                renderer.GetPropertyBlock(block, _MaterialIndex);
        }

        protected void SetBlock(Renderer renderer, MaterialPropertyBlock block)
        {
            if (_MaterialIndex < 0)
                renderer.SetPropertyBlock(block);
            else
                renderer.SetPropertyBlock(block, _MaterialIndex);
        }

        protected Material GetMaterial(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            var index = _MaterialIndex < 0 ? 0 : _MaterialIndex;
            return index < materials.Length ? materials[index] : null;
        }

#if UNITY_EDITOR
        protected abstract IEnumerable GetMaterialPropertyNames(InspectorProperty property);
        private void ClearPropertyBlock(InspectorProperty property) => RendererPropertyTweenActionUtils.ClearPropertyBlock(property, _MaterialIndex);
#endif
    }

    [Serializable]
    public class RendererTextureOffsetTweenAction : ARendererMaterialTweenAction<Renderer>
    {
        [SerializeField]
        private Vector2 _EndValue;

        private static readonly int TEXTURE_ID = Shader.PropertyToID("_MainTex");

        protected override Tween GetTween(Material material)
        {
            return DOTween.To(() => material.GetTextureOffset(TEXTURE_ID), x => material.SetTextureOffset(TEXTURE_ID, x), _EndValue, _Duration);
        }
    }

    [Serializable]
    public class RendererFadeTweenAction : ARendererMaterialTweenAction<Renderer>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Material target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class RendererColorTweenAction : ARendererMaterialTweenAction<Renderer>
    {
        [ColorUsage(true, true)]
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Material target) => target.DOColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class RendererColorPropertyTweenAction : ARendererMaterialTweenAction<Renderer>
    {
        [SerializeField]
        [EnhancedValueDropdown("GetMaterialPropertyNames", AppendNextDrawer = true)]
        [InlineIconButton(FontAwesomeEditorIconType.BroomSolid, "ClearPropertyBlock", "Clear property block")]
        private string _Property;

        [ColorUsage(true, true)]
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Material target) => target.DOColor(_EndColor, _Property, _Duration);
        
#if UNITY_EDITOR
        protected IEnumerable GetMaterialPropertyNames(InspectorProperty property) => 
            RendererPropertyTweenActionUtils.GetMaterialPropertyNames(property, _MaterialIndex, ShaderPropertyType.Color);
#endif
    }
    
    [Serializable]
    public class RendererFloatPropertyTweenAction : ARendererMaterialTweenAction<Renderer>
    {
        [SerializeField]
        [EnhancedValueDropdown("GetMaterialPropertyNames", AppendNextDrawer = true)]
        [InlineIconButton(FontAwesomeEditorIconType.BroomSolid, "ClearPropertyBlock", "Clear property block")]
        private string _Property;

        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Material target) => target.DOFloat(_EndValue, _Property, _Duration);
        
#if UNITY_EDITOR
        protected IEnumerable GetMaterialPropertyNames(InspectorProperty property) => 
            RendererPropertyTweenActionUtils.GetMaterialPropertyNames(property, _MaterialIndex, ShaderPropertyType.Float, ShaderPropertyType.Range);
#endif
    }
    
    [Serializable]
    public class RendererColorPropertyBlockTweenAction : ARendererPropertyBlockTweenAction<Renderer>
    {
        [ColorUsage(true, true)]
        [SerializeField]
        private Color _EndColor;
        
        protected override Tween GetTween(Renderer target)
        {
            var id = Shader.PropertyToID(_Property);
            var block = new MaterialPropertyBlock();
            var tween = DOTween.To(
                () =>
                {
                    GetBlock(target, block);
                    if (block.HasColor(id))
                        return block.GetColor(id);
                    var material = GetMaterial(target);
                    return material != null ? material.GetColor(id) : Color.white;
                },
                x =>
                {
                    GetBlock(target, block);
                    block.SetColor(id, x);
                    SetBlock(target, block);
                },
                _EndColor, _Duration);
            return tween;
        }
        
#if UNITY_EDITOR
        protected override IEnumerable GetMaterialPropertyNames(InspectorProperty property) => 
            RendererPropertyTweenActionUtils.GetMaterialPropertyNames(property, _MaterialIndex, ShaderPropertyType.Color);
#endif
    }

    [Serializable]
    public class RendererFloatPropertyBlockTweenAction : ARendererPropertyBlockTweenAction<Renderer>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Renderer target)
        {
            var id = Shader.PropertyToID(_Property);
            var block = new MaterialPropertyBlock();
            var tween = DOTween.To(
                () =>
                {
                    GetBlock(target, block);
                    if (block.HasFloat(id))
                        return block.GetFloat(id);
                    var material = GetMaterial(target);
                    return material != null ? material.GetFloat(id) : 0f;
                },
                x =>
                {
                    GetBlock(target, block);
                    block.SetFloat(id, x);
                    SetBlock(target, block);
                },
                _EndValue, _Duration);
            return tween;
        }
        
#if UNITY_EDITOR
        protected override IEnumerable GetMaterialPropertyNames(InspectorProperty property) => 
            RendererPropertyTweenActionUtils.GetMaterialPropertyNames(property, _MaterialIndex, ShaderPropertyType.Float, ShaderPropertyType.Range);
#endif
    }
}
