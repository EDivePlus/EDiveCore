using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class LightColorTweenAction : ATweenObjectAction<Light>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Light target) => target.DOColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class LightBlendableColorTweenAction : ATweenObjectAction<Light>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Light target) => target.DOBlendableColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class LightIntensityTweenAction : ATweenObjectAction<Light>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Light target) => target.DOIntensity(_EndValue, _Duration);
    }
    
    [Serializable]
    public class LightShadowStrengthTweenAction : ATweenObjectAction<Light>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Light target) => target.DOShadowStrength(_EndValue, _Duration);
    }

}