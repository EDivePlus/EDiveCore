// Author: František Holubec
// Created: 12.10.2026

using System;
using DG.Tweening;
using EDIVE.Tweening.ObjectActions;
using UnityEngine;

namespace EDIVE.UIElements.SDFShapes
{
    // Goes per corner at the current size, then lands on the exact value so its mode sticks
    [Serializable]
    public class SDFGraphicRoundnessTweenAction : ATweenObjectAction<SDFGraphic>
    {
        [SerializeField]
        private CornerRoundness _EndValue;

        protected override Tween GetTween(SDFGraphic target)
        {
            var from = Vector4.zero;
            var to = Vector4.zero;
            var started = false;
            return DOTween.To(() => 0f, t =>
                {
                    if (!started)
                    {
                        started = true;
                        var rect = target.rectTransform.rect;
                        from = target.Roundness.Resolve(rect.width, rect.height);
                        to = _EndValue.Resolve(rect.width, rect.height);
                    }
                    var r = Vector4.LerpUnclamped(from, to, t);
                    target.Roundness = CornerRoundness.PerCorner(r.z, r.x, r.y, r.w);
                }, 1f, _Duration)
                .OnComplete(() => target.Roundness = _EndValue)
                .SetTarget(target);
        }
    }

    [Serializable]
    public class SDFOutlineColorTweenAction : ATweenObjectAction<SDFOutline>
    {
        [SerializeField]
        private Color _EndColor = Color.black;

        protected override Tween GetTween(SDFOutline target) => DOTween.To(() => target.Color, x => target.Color = x, _EndColor, _Duration).SetTarget(target);
    }

    [Serializable]
    public class SDFOutlineSizeTweenAction : ATweenObjectAction<SDFOutline>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(SDFOutline target) => DOTween.To(() => target.Size, x => target.Size = x, _EndValue, _Duration).SetTarget(target);
    }

    [Serializable]
    public class SDFShadowColorTweenAction : ATweenObjectAction<SDFShadow>
    {
        [SerializeField]
        private Color _EndColor = Color.black;

        protected override Tween GetTween(SDFShadow target) => DOTween.To(() => target.Color, x => target.Color = x, _EndColor, _Duration).SetTarget(target);
    }

    [Serializable]
    public class SDFShadowSizeTweenAction : ATweenObjectAction<SDFShadow>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(SDFShadow target) => DOTween.To(() => target.Size, x => target.Size = x, _EndValue, _Duration).SetTarget(target);
    }

    [Serializable]
    public class SDFShadowBlurTweenAction : ATweenObjectAction<SDFShadow>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(SDFShadow target) => DOTween.To(() => target.Blur, x => target.Blur = x, _EndValue, _Duration).SetTarget(target);
    }

    [Serializable]
    public class SDFArcValueTweenAction : ATweenObjectAction<SDFArc>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(SDFArc target) => DOTween.To(() => target.Value, x => target.Value = x, _EndValue, _Duration).SetTarget(target);
    }
}
