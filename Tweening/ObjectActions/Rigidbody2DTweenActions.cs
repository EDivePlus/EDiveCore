using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class Rigidbody2DMoveTweenAction : ATweenObjectAction<Rigidbody2D>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody2D target) => target.DOMove(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class Rigidbody2DMoveXTweenAction : ATweenObjectAction<Rigidbody2D>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody2D target) => target.DOMoveX(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class Rigidbody2DMoveYTweenAction : ATweenObjectAction<Rigidbody2D>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody2D target) => target.DOMoveY(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class Rigidbody2DJumpTweenAction : ATweenObjectAction<Rigidbody2D>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private float _JumpPower;
        
        [SerializeField]
        private int _NumJumps;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody2D target) => target.DOJump(_EndValue, _JumpPower, _NumJumps, _Duration, _Snapping);
    }
    
    [Serializable]
    public class Rigidbody2DRotateTweenAction : ATweenObjectAction<Rigidbody2D>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(Rigidbody2D target) => target.DORotate(_EndValue, _Duration);
    }
}