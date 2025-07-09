// Author: František Holubec
// Created: 18.05.2025

using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class RectTransformExtensions
    {
        public enum AnchorPreset
        {
            TopLeft,
            TopCenter,
            TopRight,

            MidLeft,
            MidCenter,
            MiddleRight,

            BottomLeft,
            BottomCenter,
            BottomRight,

            VerticalStretchLeft,
            VerticalStretchRight,
            VerticalStretchCenter,

            HorizontalStretchTop,
            HorizontalStretchMiddle,
            HorizontalStretchBottom,

            StretchAll
        }

        public enum PivotPreset
        {
            TopLeft,
            TopCenter,
            TopRight,

            MiddleLeft,
            MiddleCenter,
            MiddleRight,

            BottomLeft,
            BottomCenter,
            BottomRight,
        }

        public static void ResetOffsets(this RectTransform rectTransform)
        {
            if (!rectTransform) return;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        public static void SetAnchor(this RectTransform rectTransform, AnchorPreset anchorPreset)
        {
            SetAnchor(rectTransform, anchorPreset, Vector2.zero);
        }

        public static void SetAnchor(this RectTransform rectTransform, AnchorPreset anchorPreset, Vector2 positionOffset)
        {
            if (!rectTransform) return;
            (rectTransform.anchorMin, rectTransform.anchorMax) = anchorPreset switch
            {
                AnchorPreset.TopLeft => (new Vector2(0, 1), new Vector2(0, 1)),
                AnchorPreset.TopCenter => (new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
                AnchorPreset.TopRight => (new Vector2(1, 1), new Vector2(1, 1)),
                AnchorPreset.MidLeft => (new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
                AnchorPreset.MidCenter => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
                AnchorPreset.MiddleRight => (new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
                AnchorPreset.BottomLeft => (new Vector2(0, 0), new Vector2(0, 0)),
                AnchorPreset.BottomCenter => (new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
                AnchorPreset.BottomRight => (new Vector2(1, 0), new Vector2(1, 0)),
                AnchorPreset.HorizontalStretchTop => (new Vector2(0, 1), new Vector2(1, 1)),
                AnchorPreset.HorizontalStretchMiddle => (new Vector2(0, 0.5f), new Vector2(1, 0.5f)),
                AnchorPreset.HorizontalStretchBottom => (new Vector2(0, 0), new Vector2(1, 0)),
                AnchorPreset.VerticalStretchLeft => (new Vector2(0, 0), new Vector2(0, 1)),
                AnchorPreset.VerticalStretchCenter => (new Vector2(0.5f, 0), new Vector2(0.5f, 1)),
                AnchorPreset.VerticalStretchRight => (new Vector2(1, 0), new Vector2(1, 1)),
                AnchorPreset.StretchAll => (new Vector2(0, 0), new Vector2(1, 1)),
                _ => (rectTransform.anchorMin, rectTransform.anchorMax)
            };
            rectTransform.anchoredPosition = positionOffset;
        }

        public static void SetPivot(this RectTransform rectTransform, PivotPreset preset)
        {
            if (!rectTransform) return;
            rectTransform.pivot = preset switch
            {
                PivotPreset.TopLeft => new Vector2(0, 1),
                PivotPreset.TopCenter => new Vector2(0.5f, 1),
                PivotPreset.TopRight => new Vector2(1, 1),
                PivotPreset.MiddleLeft => new Vector2(0, 0.5f),
                PivotPreset.MiddleCenter => new Vector2(0.5f, 0.5f),
                PivotPreset.MiddleRight => new Vector2(1, 0.5f),
                PivotPreset.BottomLeft => new Vector2(0, 0),
                PivotPreset.BottomCenter => new Vector2(0.5f, 0),
                PivotPreset.BottomRight => new Vector2(1, 0),
                _ => rectTransform.pivot
            };
        }

        public static void SetToFillParent(this RectTransform rectTransform)
        {
            if (!rectTransform) return;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            rectTransform.offsetMin = rectTransform.offsetMax = rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
        }
    }
}
