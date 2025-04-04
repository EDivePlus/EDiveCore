using System;
using DG.Tweening;
using EDIVE.DataStructures.RectTransformPreset;
using EDIVE.DataStructures.RectTransformSnapshot;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class RectTransformAnchorPosTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private Vector3 _EndValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private RectTransform _Target;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorPos(GetEndValue(), _Duration, _Snapping);
        
        private Vector3 GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndValue,
            PositionTargetType.Transform => _Target.anchoredPosition,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    [Serializable]
    public class RectTransformAnchorPosXTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private float _EndXValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private RectTransform _Target;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorPosX(GetEndValue(), _Duration, _Snapping);        
        
        private float GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndXValue,
            PositionTargetType.Transform => _Target.anchoredPosition.x,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    [Serializable]
    public class RectTransformAnchorPosYTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private PositionTargetType _TargetType;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Value)]
        private float _EndYValue;

        [SerializeField]
        [ShowIf(nameof(_TargetType), PositionTargetType.Transform)]
        private RectTransform _Target;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorPosY(GetEndValue(), _Duration, _Snapping);
                
        private float GetEndValue() => _TargetType switch 
        {
            PositionTargetType.Value => _EndYValue,
            PositionTargetType.Transform => _Target.anchoredPosition.y,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [Serializable]
    public class RectTransformRelativeMoveAnchorPosTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _EndValue;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorPos(target.anchoredPosition + _EndValue, _Duration, _Snapping);
    }

    [Serializable]
    public class RectTransformRelativeMoveAnchorPosXTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorPosX(target.anchoredPosition.x + _EndValue, _Duration, _Snapping);
    }

    [Serializable]
    public class RectTransformRelativeMoveAnchorPosYTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private float _EndValue;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorPosY(target.anchoredPosition.y + _EndValue, _Duration, _Snapping);
    }

    [Serializable]
    public class RectTransformAnchorMinTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _EndValue;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorMin(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RectTransformAnchorMaxTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _EndValue;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOAnchorMax(_EndValue, _Duration, _Snapping);
    }
    
    [Serializable]
    public class RectTransformJumpAnchorPosTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        [SerializeField]
        private float _JumpPower;
        
        [SerializeField]
        private int _NumJumps;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOJumpAnchorPos(_EndValue, _JumpPower, _NumJumps, _Duration, _Snapping);
    }
    
    
    [Serializable]
    public class RectTransformPivotTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _EndValue;
        
        protected override Tween GetTween(RectTransform target) => target.DOPivot(_EndValue, _Duration);
    }
    
    
    [Serializable]
    public class RectTransformPivotXTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(RectTransform target) => target.DOPivotX(_EndValue, _Duration);
    }
    
    
    [Serializable]
    public class RectTransformPivotYTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(RectTransform target) => target.DOPivotY(_EndValue, _Duration);
    }
    
    [Serializable]
    public class RectTransformPunchAnchorPosTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _Punch;
        
        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Elasticity= 1;
        
        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOPunchAnchorPos(_Punch, _Duration, _Vibrato, _Elasticity, _Snapping);
    }
    
    [Serializable]
    public class RectTransformShakeAnchorPosTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private float _Strength = 3;

        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Randomness = 90;

        [SerializeField]
        private bool _FadeOut = true;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOShakeAnchorPos(_Duration, _Strength, _Vibrato, _Randomness, _Snapping, _FadeOut);
    }
    
    [Serializable]
    public class RectTransformSizeDeltaTweenAction : ATweenObjectAction<RectTransform>
    {
        [SerializeField]
        private Vector2 _EndValue;

        [SerializeField]
        private bool _Snapping;

        protected override Tween GetTween(RectTransform target) => target.DOSizeDelta(_EndValue, _Duration, _Snapping);
    }

    [Serializable]
    public class RectTransformSnapshotPreset : ATweenObjectAction<RectTransform>
    {
        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private RectTransformSnapshot _Snapshot;

        protected override Tween GetTween(RectTransform target)
        {
            return target.DOMorph(_Snapshot, _Duration);
        }
    }
}