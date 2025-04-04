using DG.Tweening;
using UnityEngine;

namespace EDIVE.DataStructures.RectTransformPreset
{
    public static class RectTransformSnapshotExtensions
    {
        public static Tween DOMorph(this RectTransform t, RectTransformSnapshot.RectTransformSnapshot preset, float duration)
        {
            var sequence = DOTween.Sequence()
                .Append(t.DOAnchorPos(preset.AnchoredPosition, duration))
                .Join(t.DOPivot(preset.Pivot, duration))
                .Join(t.DOAnchorMax(preset.AnchorMax, duration))
                .Join(t.DOAnchorMin(preset.AnchorMin, duration))
                .Join(t.DOSizeDelta(preset.SizeDelta, duration))
                .Join(t.DORotateQuaternion(preset.Rotation, duration))
                .Join(t.DOScale(preset.LocalScale, duration));

            return sequence;
        }
    }
}
