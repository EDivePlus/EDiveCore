// Author: František Holubec
// Created: 14.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.View.EditorPreview
{
    public enum PreviewEdge
    {
        Bottom,
        Top,
        Left,
        Right,
        Center,
        Fill
    }

    // Stand-in for host UI around the edited prefab, e.g. a taskbar
    [Serializable]
    [EnhancedTypeSelector(true, 1)]
    public abstract class APreviewPlaceholder
    {
        [SerializeField]
        private PreviewEdge _Edge = PreviewEdge.Bottom;

        [Tooltip("Size along the edge, 0 stretches")]
        [SerializeField]
        private float _Length;

        [SerializeField]
        private float _Thickness = 60;

        [SerializeField]
        private float _Margin;

        [Tooltip("Placed inside the safe area")]
        [SerializeField]
        private bool _InSafeArea = true;

        [Tooltip("Inset content keeps clear of it")]
        [SerializeField]
        private bool _ReservesSpace = true;

        public PreviewEdge Edge => _Edge;
        public bool InSafeArea => _InSafeArea;
        public bool ReservesSpace => _ReservesSpace;
        public float ReservedSize => _Thickness + _Margin;

        public abstract string Title { get; }

        protected APreviewPlaceholder() { }
        protected APreviewPlaceholder(PreviewEdge edge, float length, float thickness, float margin)
        {
            _Edge = edge;
            _Length = length;
            _Thickness = thickness;
            _Margin = margin;
        }

        public void Layout(RectTransform rect)
        {
            switch (_Edge)
            {
                case PreviewEdge.Fill:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = new Vector2(_Margin, _Margin);
                    rect.offsetMax = new Vector2(-_Margin, -_Margin);
                    return;
                case PreviewEdge.Center:
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(_Length, _Thickness);
                    rect.anchoredPosition = Vector2.zero;
                    return;
                case PreviewEdge.Bottom:
                case PreviewEdge.Top:
                case PreviewEdge.Left:
                case PreviewEdge.Right:
                default:
                    break;
            }

            var side = _Edge is PreviewEdge.Bottom or PreviewEdge.Left ? 0f : 1f;
            var inward = side == 0f ? _Margin : -_Margin;
            var stretch = _Length <= 0;
            var alongMin = stretch ? 0f : 0.5f;
            var alongMax = stretch ? 1f : 0.5f;
            var alongSize = stretch ? -2 * _Margin : _Length;

            if (_Edge is PreviewEdge.Bottom or PreviewEdge.Top)
            {
                rect.anchorMin = new Vector2(alongMin, side);
                rect.anchorMax = new Vector2(alongMax, side);
                rect.pivot = new Vector2(0.5f, side);
                rect.sizeDelta = new Vector2(alongSize, _Thickness);
                rect.anchoredPosition = new Vector2(0, inward);
            }
            else
            {
                rect.anchorMin = new Vector2(side, alongMin);
                rect.anchorMax = new Vector2(side, alongMax);
                rect.pivot = new Vector2(side, 0.5f);
                rect.sizeDelta = new Vector2(_Thickness, alongSize);
                rect.anchoredPosition = new Vector2(inward, 0);
            }
        }

        public abstract void Build(RectTransform rect);
    }
}
