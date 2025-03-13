using System;
using DG.Tweening;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.Tweening
{
    [Serializable]
    [InlineProperty]
    public class EaseBundle
    {
        [HideLabel]
        [HorizontalGroup("Ease", 70)]
        [SerializeField]
        private EaseType _EaseType;

        [ShowIf(nameof(_EaseType), EaseType.Ease)]
        [HideLabel]
        [InlineIconButton(FontAwesomeEditorIconType.LeftRightSolid, "InvertAction")]
        [HorizontalGroup("Ease")]
        [SerializeField]
        private Ease _Ease = Ease.Linear;

        [ShowIf(nameof(_EaseType), EaseType.Curve)]
        [HideLabel]
        [InlineIconButton(FontAwesomeEditorIconType.LeftRightSolid, "InvertAction")]
        [HorizontalGroup("Ease")]
        [SerializeField]
        private AnimationCurve _Curve;
        
        [ShowIf(nameof(IsElasticEase))]
        [SerializeField]
        [InlineIconButton(FontAwesomeEditorIconType.RotateSolid, "ResetTo10Percent")]
        [MinValue(0)]
        [Indent]
        private float _Amplitude = 1.70158f;
        
        [ShowIf(nameof(IsElasticEase))]
        [SerializeField]
        [MinValue(0)]
        [Indent]
        private float _Period = 0;
        
        [ShowIf(nameof(IsBackEase))]
        [SerializeField]
        [InlineIconButton(FontAwesomeEditorIconType.RotateSolid, "ResetTo10Percent")]
        [MinValue(0)]
        [Indent]
        private float _Overshoot = 1.70158f;
        
        [Tooltip("Total number of flashes to apply. An even number will end the tween on the starting value, while an odd one will end it on the end value.")]
        [ShowIf(nameof(IsFlashEase))]
        [SerializeField]
        [MinValue(0)]
        [Indent]
        private int _Count = 1;
        
        [Tooltip("Power in time of the ease. 0 is balanced, 1 fully weakens the ease in time, -1 starts the ease fully weakened and gives it power towards the end.")]
        [ShowIf(nameof(IsFlashEase))]
        [SerializeField]
        [Range(-1, 1)]
        [Indent]
        private float _Power = 0;

        public EaseType EaseType => _EaseType;
        public Ease EaseFunction => _Ease != Ease.Unset ? _Ease : Ease.Linear;
        public AnimationCurve AnimationCurve => _Curve;
        
        private bool IsFlashEase => EaseType == EaseType.Ease && EaseFunction.IsOneOf(Ease.Flash, Ease.InFlash, Ease.OutFlash, Ease.InOutFlash);
        private bool IsBackEase => EaseType == EaseType.Ease && EaseFunction.IsOneOf(Ease.InBack, Ease.OutBack, Ease.InOutBack);
        private bool IsElasticEase => EaseType == EaseType.Ease && EaseFunction.IsOneOf(Ease.InElastic, Ease.OutElastic, Ease.InOutElastic);
        
        public EaseBundle(Ease ease)
        {
            _EaseType = EaseType.Ease;
            _Ease = ease;
        }

        public EaseBundle(AnimationCurve curve)
        {
            _EaseType = EaseType.Curve;
            _Curve = curve;
        }

        public T SetEase<T>(T tween) where T : Tween
        {
            switch (EaseType)
            {
                case EaseType.Ease:
                    if (IsElasticEase) tween.SetEase(EaseFunction, _Amplitude, _Period);
                    if (IsBackEase) tween.SetEase(EaseFunction, _Overshoot);
                    if (IsFlashEase) tween.SetEase(EaseFunction, _Count, _Power);
                    else tween.SetEase(EaseFunction);
                    break;
                case EaseType.Curve:
                    tween.SetEase(AnimationCurve);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            return tween;
        }

        public void Invert()
        {
            switch (_EaseType)
            {
                case EaseType.Curve:
                    _Curve.FlipXY();
                    break;
                case EaseType.Ease:
                    _Ease = _Ease.GetInverse();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        private void InvertAction(InspectorProperty property)
        {
            Invert();
            property.MarkSerializationRootDirty();
        }

        public void ResetTo10Percent(InspectorProperty property)
        {
            property.ValueEntry.WeakSmartValue = 1.70158f;
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
