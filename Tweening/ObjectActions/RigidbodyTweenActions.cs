using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class RigidbodyMoveTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody target) => target.DOMove(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RigidbodyMoveXTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody target) => target.DOMoveX(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RigidbodyMoveYTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody target) => target.DOMoveY(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RigidbodyMoveZTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private float _EndValue;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody target) => target.DOMoveZ(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RigidbodyJumpTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        [SerializeField]
        private float _JumpPower;
        
        [SerializeField]
        private int _NumJumps;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Rigidbody target) => target.DOJump(_EndValue, _JumpPower, _NumJumps, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RigidbodyRotateTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private RotateMode _RotateMode = RotateMode.Fast;

        protected override Tween GetTween(Rigidbody target) => target.DORotate(_EndValue, _Duration, _RotateMode);
    }
    
    [Serializable]
    public class RigidbodyLookAtTweenAction : ATweenObjectAction<Rigidbody>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        [SerializeField]
        private AxisConstraint _AxisConstraint = AxisConstraint.None;

        [SerializeField]
        private Vector3 _UpVector = Vector3.up;
        
        protected override Tween GetTween(Rigidbody target) => target.DOLookAt(_EndValue, _Duration, _AxisConstraint, _UpVector);
    }
}