using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class SpriteRendererColorTweenAction : ATweenObjectAction<SpriteRenderer>
    {
        [SerializeField]
        private Color _EndColor;
        
        protected override Tween GetTween(SpriteRenderer target) => target.DOColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class SpriteRendererFadeTweenAction : ATweenObjectAction<SpriteRenderer>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(SpriteRenderer target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class SpriteRendererGradientColorTweenAction : ATweenObjectAction<SpriteRenderer>
    {
        [SerializeField]
        private Gradient _Gradient;

        protected override Tween GetTween(SpriteRenderer target) => target.DOGradientColor(_Gradient, _Duration);
    }
    
    [Serializable]
    public class SpriteRendererBlendableColorTweenAction : ATweenObjectAction<SpriteRenderer>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(SpriteRenderer target) => target.DOBlendableColor(_EndColor, _Duration);
    }

    [Serializable]
    public class SpriteRendererSpriteSheetTweenAction : ATweenObjectAction<SpriteRenderer>
    {
        [SerializeField]
        private List<Sprite> _Sprites;

        protected override Tween GetTween(SpriteRenderer target)
        {
            var currentIndex = 0;
            var tween = DOTween.To(() => currentIndex, x => currentIndex = x, _Sprites.Count - 1, _Duration)
                .OnUpdate(() =>
                {
                    currentIndex = currentIndex.PositiveModulo(_Sprites.Count);
                    if (_Sprites[currentIndex] == null) return;
                    target.sprite = _Sprites[currentIndex];
                });
            return tween;
        }
    }
}