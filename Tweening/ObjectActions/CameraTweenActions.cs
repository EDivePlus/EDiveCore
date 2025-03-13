using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class CameraAspectTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Camera target) => target.DOAspect(_EndValue, _Duration);
    }

    [Serializable]
    public class CameraFarClipPlaneTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Camera target) => target.DOFarClipPlane(_EndValue, _Duration);
    }

    [Serializable]
    public class CameraFieldOfViewTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Camera target) => target.DOFieldOfView(_EndValue, _Duration);
    }

    [Serializable]
    public class CameraNearClipPlaneTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Camera target) => target.DONearClipPlane(_EndValue, _Duration);
    }

    [Serializable]
    public class CameraOrthoSizeTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Camera target) => target.DOOrthoSize(_EndValue, _Duration);
    }

    [Serializable]
    public class CameraColorTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Camera target) => target.DOColor(_EndColor, _Duration);
    }

    [Serializable]
    public class CameraPixelRectTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private Rect _Rect;

        protected override Tween GetTween(Camera target) => target.DOPixelRect(_Rect, _Duration);
    }

    [Serializable]
    public class CameraRectTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private Rect _Rect;

        protected override Tween GetTween(Camera target) => target.DORect(_Rect, _Duration);
    }

    [Serializable]
    public class CameraShakePositionTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private Vector3 _Strength = Vector3.one * 3;

        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Randomness = 90;

        [SerializeField]
        private bool _FadeOut = true;

        protected override Tween GetTween(Camera target) => target.DOShakePosition(_Duration, _Strength, _Vibrato, _Randomness, _FadeOut);
    }

    [Serializable]
    public class CameraShakeRotationTweenAction : ATweenObjectAction<Camera>
    {
        [SerializeField]
        private Vector3 _Strength = Vector3.one * 3;

        [SerializeField]
        private int _Vibrato = 10;

        [SerializeField]
        private float _Randomness = 90;

        [SerializeField]
        private bool _FadeOut = true;

        protected override Tween GetTween(Camera target) => target.DOShakeRotation(_Duration, _Strength, _Vibrato, _Randomness, _FadeOut);
    }
}
