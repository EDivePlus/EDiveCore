using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public abstract class ARendererMaterialTweenAction<TRenderer> : ATweenObjectAction<TRenderer> where TRenderer : Renderer
    {
        [SerializeField]
        private bool _UseSharedMaterial;

        [MinValue(0)]
        [SerializeField]
        private int _MaterialIndex;

        private bool TryGetMaterial(TRenderer target, out Material material)
        {
            var materials = _UseSharedMaterial ? target.sharedMaterials : target.materials;
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
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Material target) => target.DOColor(_EndColor, _Duration);
    }
}
