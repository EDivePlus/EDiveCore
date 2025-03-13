using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class ScrollRectNormalizedPosTweenAction : ATweenObjectAction<ScrollRect>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(ScrollRect target) => target.DONormalizedPos(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class ScrollRectHorizontalNormalizedPosTweenAction : ATweenObjectAction<ScrollRect>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(ScrollRect target) => target.DOHorizontalNormalizedPos(_EndValue, _Duration, _Snapping);
    }

    [Serializable]
    public class ScrollRectVerticalNormalizedPosTweenAction : ATweenObjectAction<ScrollRect>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(ScrollRect target) => target.DOVerticalNormalizedPos(_EndValue, _Duration, _Snapping);
    }
}