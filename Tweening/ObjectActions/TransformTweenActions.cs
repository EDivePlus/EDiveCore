using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    public enum PositionTargetType
    {
        Value,
        Transform
    }

    [Serializable]
    public class TransformMoveTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private Vector3 _EndValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private Transform _Target;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Transform target) => target.DOMove(GetEndValue(), _Duration, _Snapping);

        private Vector3 GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndValue,
            PositionTargetType.Transform => _Target.position,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [Serializable]
    public class TransformMoveXTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private float _EndValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private Transform _Target;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMoveX(GetEndValue(), _Duration, _Snapping);
        
        private float GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndValue,
            PositionTargetType.Transform => _Target.position.x,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    [Serializable]
    public class TransformMoveYTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private float _EndValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private Transform _Target;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMoveY(GetEndValue(), _Duration, _Snapping);
        
        private float GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndValue,
            PositionTargetType.Transform => _Target.position.y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    [Serializable]
    public class TransformMoveZTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private float _EndValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private Transform _Target;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMoveZ(GetEndValue(), _Duration, _Snapping);
        
        private float GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndValue,
            PositionTargetType.Transform => _Target.position.z,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    [Serializable]
    public class TransformLocalMoveTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOLocalMove(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformLocalMoveXTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOLocalMoveX(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformLocalMoveYTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOLocalMoveY(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformLocalMoveZTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOLocalMoveZ(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformRelativeMoveTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMove(target.position + _EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformRelativeMoveXTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMoveX(target.position.x + _EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformRelativeMoveYTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMoveY(target.position.y + _EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformRelativeMoveZTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOMoveZ(target.position.z + _EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformScaleTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        protected override Tween GetTween(Transform target) => target.DOScale(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TransformScaleXTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(Transform target) => target.DOScaleX(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TransformScaleYTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(Transform target) => target.DOScaleY(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TransformScaleZTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(Transform target) => target.DOScaleZ(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TransformJumpTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private Vector3 _EndValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private Transform _Target;
        
        [SerializeField]
        private float _JumpPower;
        
        [SerializeField]
        private int _NumJumps;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOJump(GetEndValue(), _JumpPower, _NumJumps, _Duration, _Snapping);
        
        private Vector3 GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndValue,
            PositionTargetType.Transform => _Target.position,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    [Serializable]
    public class TransformLocalJumpTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        [SerializeField]
        private float _JumpPower;
        
        [SerializeField]
        private int _NumJumps;
        
        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOLocalJump(_EndValue, _JumpPower, _NumJumps, _Duration, _Snapping);
    }

    [Serializable]
    public class TransformRotateTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        [SerializeField]
        private RotateMode _RotateMode;
        
        protected override Tween GetTween(Transform target) => target.DORotate(_EndValue, _Duration, _RotateMode);
    }
    
    [Serializable]
    public class TransformRotateQuaternionTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Quaternion _EndValue;
        
        protected override Tween GetTween(Transform target) => target.DORotateQuaternion(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TransformLocalRotateTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;
        
        [SerializeField]
        private RotateMode _RotateMode;
        
        protected override Tween GetTween(Transform target) => target.DOLocalRotate(_EndValue, _Duration, _RotateMode);
    }
    
    [Serializable]
    public class TransformLocalRotateQuaternionTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Quaternion _EndValue;
        
        protected override Tween GetTween(Transform target) => target.DOLocalRotateQuaternion(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TransformLookAtTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;
        
        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private Vector3 _Towards;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private Transform _Target;

        [SerializeField]
        private AxisConstraint _AxisConstraint = AxisConstraint.None;

        [SerializeField]
        private Vector3 _UpVector = Vector3.up;
        
        protected override Tween GetTween(Transform target) => target.DOLookAt(GetEndValue(), _Duration, _AxisConstraint, _UpVector);
        
        private Vector3 GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _Towards,
            PositionTargetType.Transform => _Target.position,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [Serializable]
    public class TransformPunchPositionTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _Punch;
        
        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Elasticity= 1;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Transform target) => target.DOPunchPosition(_Punch, _Duration, _Vibrato, _Elasticity, _Snapping);
    }
    
    [Serializable]
    public class TransformPunchRotationTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _Punch;
        
        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Elasticity= 1;

        protected override Tween GetTween(Transform target) => target.DOPunchRotation(_Punch, _Duration, _Vibrato, _Elasticity);
    }
    
    [Serializable]
    public class TransformPunchScaleTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _Punch;
        
        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Elasticity= 1;

        protected override Tween GetTween(Transform target) => target.DOPunchScale(_Punch, _Duration, _Vibrato, _Elasticity);
    }
    
    [Serializable]
    public class TransformShakePositionTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _Strength = Vector3.one * 3;

        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Randomness = 90;

        [SerializeField]
        private bool _FadeOut = true;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(Transform target) => target.DOShakePosition(_Duration, _Strength, _Vibrato, _Randomness, _Snapping, _FadeOut);
    }
    
    [Serializable]
    public class TransformShakeRotationTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _Strength = Vector3.one * 3;

        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Randomness = 90;

        [SerializeField]
        private bool _FadeOut = true;
        
        protected override Tween GetTween(Transform target) => target.DOShakeRotation(_Duration, _Strength, _Vibrato, _Randomness, _FadeOut);
    }
    
    [Serializable]
    public class TransformShakeScaleTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private float _Strength = 3;

        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Randomness = 90;

        [SerializeField]
        private bool _FadeOut = true;

        protected override Tween GetTween(Transform target) => target.DOShakeScale(_Duration, _Strength, _Vibrato, _Randomness, _FadeOut);
    }

    [Serializable]
    public class TransformBlendableMoveByTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOBlendableMoveBy(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformBlendableLocalMoveByTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOBlendableLocalMoveBy(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformPathTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOBlendableLocalMoveBy(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformBlendableRotateByTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _ByValue;

        [SerializeField]
        private RotateMode _RotateMode = RotateMode.Fast;
        
        protected override Tween GetTween(Transform target) => target.DOBlendableRotateBy(_ByValue, _Duration, _RotateMode);
    }
    
    [Serializable]
    public class TransformBlendableLocalRotateByTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _ByValue;

        [SerializeField]
        private RotateMode _RotateMode = RotateMode.Fast;
        
        protected override Tween GetTween(Transform target) => target.DOBlendableLocalRotateBy(_ByValue, _Duration, _RotateMode);
    }
    
    [Serializable]
    public class TransformBlendableScaleByTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        [SerializeField]
        private bool _Snapping;
        
        protected override Tween GetTween(Transform target) => target.DOBlendableLocalMoveBy(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class TransformLocalPathTweenAction : ATweenObjectAction<Transform>
    {
        [SerializeField]
        private Vector3 _EndValue;

        protected override Tween GetTween(Transform target) => target.DOBlendableScaleBy(_EndValue, _Duration);
    }
}