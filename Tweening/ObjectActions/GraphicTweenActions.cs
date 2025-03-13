using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class GraphicFadeTweenAction : ATweenObjectAction<Graphic>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Graphic target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class GraphicColorTweenAction : ATweenObjectAction<Graphic>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Graphic target) => target.DOColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class GraphicBlendableColorTweenAction : ATweenObjectAction<Graphic>
    {
        [SerializeField]
        private Color _EndColor;
        
        protected override Tween GetTween(Graphic target) => target.DOBlendableColor(_EndColor, _Duration);
    }
}