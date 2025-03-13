using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class RendererTextureOffsetTweenAction : ATweenObjectAction<Renderer>
    {
        [SerializeField]
        private Vector2 _EndValue;

        [SerializeField]
        private bool _UseSharedMaterial;

        private static readonly int TEXTURE_ID = Shader.PropertyToID("_MainTex");

        protected override Tween GetTween(Renderer target)
        {
            var material = _UseSharedMaterial ? target.sharedMaterial : target.material;
            var tween = DOTween.To(() => material.GetTextureOffset(TEXTURE_ID), x => material.SetTextureOffset(TEXTURE_ID, x), _EndValue, _Duration);
            return tween;
        }
    }
}
