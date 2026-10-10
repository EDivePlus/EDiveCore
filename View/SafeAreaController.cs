using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.View
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaController : MonoBehaviour
    {
        [Flags]
        private enum SafeAreaUpdateSide
        {
            Left = 1 << 0,
            Right = 1 << 1,
            Top = 1 << 2,
            Bottom = 1 << 3,
            All = Left | Right | Top | Bottom
        }

        [Flags]
        private enum SafeAreaAlignment
        {
            None = 0,
            CenterHorizontally = 1 << 0,
            CenterVertically = 1 << 1
        }

        [EnhancedInfoBox("World Space canvas. Safe area not applied.", ShowIf = nameof(IsWorldSpace))]
        [EnhancedInfoBox("Layout group on parent or size fitter here also drives this rect. Move it to a child.", InfoMessageType.Warning, ShowIf = nameof(HasLayoutConflict))]
        [SerializeField]
        private SafeAreaUpdateSide _UpdateSides = SafeAreaUpdateSide.All;

        [Tooltip("Same inset on both sides. Keeps content centered.")]
        [SerializeField]
        private SafeAreaAlignment _Alignment = SafeAreaAlignment.None;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private DrivenRectTransformTracker _tracker;
        private bool _isDriven;

        private RectTransform RectTransform => _rectTransform ? _rectTransform : _rectTransform = (RectTransform) transform;

        private void OnEnable()
        {
            _canvas = GetComponentInParent<Canvas>(true);
            Refresh();
        }

        // Driven anchors save as zero, so fill parent when off
        private void OnDisable()
        {
            SetDriven(false);
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;
        }

        private void OnTransformParentChanged()
        {
            _canvas = GetComponentInParent<Canvas>(true);
        }

        private void Update()
        {
            Refresh();
        }

        [Button]
        private void Refresh()
        {
            if (!TryGetAnchors(out var anchorMin, out var anchorMax))
            {
                SetDriven(false);
                return;
            }

            SetDriven(true);
            var rectTransform = RectTransform;
            if (rectTransform.anchorMin != anchorMin) rectTransform.anchorMin = anchorMin;
            if (rectTransform.anchorMax != anchorMax) rectTransform.anchorMax = anchorMax;
        }

        private bool TryGetAnchors(out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;

            if (!_canvas || IsWorldSpace() || transform.parent is not RectTransform parent)
                return false;

            var parentRect = parent.rect;
            if (parentRect.width <= 0 || parentRect.height <= 0)
                return false;

            // Safe area screen corners -> parent local -> anchors, clamped to parent
            var rootCanvas = _canvas.rootCanvas;
            var canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            var safeArea = SafeAreaUtility.GetSafeArea();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safeArea.min, canvasCamera, out var localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safeArea.max, canvasCamera, out var localMax);

            anchorMin = Vector2.Max((localMin - parentRect.min) / parentRect.size, Vector2.zero);
            anchorMax = Vector2.Min((localMax - parentRect.min) / parentRect.size, Vector2.one);

            if (!ShouldUpdateSide(SafeAreaUpdateSide.Left)) anchorMin.x = 0;
            if (!ShouldUpdateSide(SafeAreaUpdateSide.Bottom)) anchorMin.y = 0;
            if (!ShouldUpdateSide(SafeAreaUpdateSide.Right)) anchorMax.x = 1;
            if (!ShouldUpdateSide(SafeAreaUpdateSide.Top)) anchorMax.y = 1;

            if (HasAlignment(SafeAreaAlignment.CenterHorizontally))
            {
                var inset = Mathf.Max(anchorMin.x, 1 - anchorMax.x);
                anchorMin.x = inset;
                anchorMax.x = 1 - inset;
            }

            if (HasAlignment(SafeAreaAlignment.CenterVertically))
            {
                var inset = Mathf.Max(anchorMin.y, 1 - anchorMax.y);
                anchorMin.y = inset;
                anchorMax.y = 1 - inset;
            }

            return true;
        }

        private void SetDriven(bool driven)
        {
            if (_isDriven == driven) return;
            _isDriven = driven;
            _tracker.Clear();
            if (driven) _tracker.Add(this, RectTransform, DrivenTransformProperties.Anchors);
        }

        private bool IsWorldSpace()
        {
            var canvas = _canvas ? _canvas : GetComponentInParent<Canvas>(true);
            return canvas && canvas.rootCanvas.renderMode == RenderMode.WorldSpace;
        }

        private bool HasLayoutConflict()
        {
            if (GetComponent<ILayoutSelfController>() != null)
                return true;

            if (GetComponent<ILayoutIgnorer>() is { ignoreLayout: true })
                return false;

            return transform.parent && transform.parent.GetComponent<ILayoutGroup>() != null;
        }

        private bool ShouldUpdateSide(SafeAreaUpdateSide side)
        {
            return (_UpdateSides & side) != 0;
        }

        private bool HasAlignment(SafeAreaAlignment alignment)
        {
            return (_Alignment & alignment) != 0;
        }
    }
}
