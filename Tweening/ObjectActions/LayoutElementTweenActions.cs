using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class LayoutElementFlexibleSizeTweenAction : ATweenObjectAction<LayoutElement>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(LayoutElement target) => target.DOFlexibleSize(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class LayoutElementMinSizeTweenAction : ATweenObjectAction<LayoutElement>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(LayoutElement target) => target.DOMinSize(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class LayoutElementPreferredSizeTweenAction : ATweenObjectAction<LayoutElement>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(LayoutElement target) => target.DOPreferredSize(_EndValue, _Duration, _Snapping);
    }
}