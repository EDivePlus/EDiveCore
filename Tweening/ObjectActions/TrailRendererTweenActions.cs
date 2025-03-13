using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class TrailRendererResizeTweenAction : ATweenObjectAction<TrailRenderer>
    {
        [SerializeField]
        private float _ToStartWidth;
        
        [SerializeField]
        private float _ToEndWidth;

        protected override Tween GetTween(TrailRenderer target) => target.DOResize(_ToStartWidth, _ToEndWidth, _Duration);
    }
    
    [Serializable]
    public class TrailRendererTimeTweenAction : ATweenObjectAction<TrailRenderer>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(TrailRenderer target) => target.DOTime(_EndValue, _Duration);
    }
}