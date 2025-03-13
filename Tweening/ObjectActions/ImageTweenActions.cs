using System;
using DG.Tweening;
using EDIVE.DataStructures;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class ImageFadeTweenAction : ATweenObjectAction<Image>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Image target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class ImageFillAmountTweenAction : ATweenObjectAction<Image>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Image target) => target.DOFillAmount(_EndValue, _Duration);
    }
    
    [Serializable]
    public class ImageGradientColorTweenAction : ATweenObjectAction<Image>
    {
        [SerializeField]
        private Gradient _Gradient;

        protected override Tween GetTween(Image target) => target.DOGradientColor(_Gradient, _Duration);
    }
    
    [Serializable]
    public class ImageColorTweenAction : ATweenObjectAction<Image>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Image target) => target.DOColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class ImageBlendableColorTweenAction : ATweenObjectAction<Image>
    {
        [SerializeField]
        private Color _EndColor;
        
        protected override Tween GetTween(Image target) => target.DOBlendableColor(_EndColor, _Duration);
    }

    [Serializable]
    public class ImageSpriteSheetTweenAction : ATweenObjectAction<Image>
    {
        [SerializeField]
        private SpriteSheetDefinition _SpriteSheet;

        protected override Tween GetTween(Image target)
        {
            var currentIndex = 0;
            var tween = DOTween.To(() => currentIndex, x => currentIndex = x, _SpriteSheet.Length - 1, _Duration)
                .OnUpdate(() =>
                {
                    var sprite = _SpriteSheet.GetSprite(currentIndex);
                    if (sprite != null)
                        target.sprite = sprite;
                });
            return tween;
        }
    }
}