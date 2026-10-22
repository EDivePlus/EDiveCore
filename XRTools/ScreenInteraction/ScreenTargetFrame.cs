// Author: Michal Petr
// Created: 22.10.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Frames the targeted interactable on screen, sized to its renderers
    [RequireComponent(typeof(Graphic))]
    public class ScreenTargetFrame : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private ScreenInteractionController _Controller;

        [SerializeField]
        [MinValue(0f)]
        private float _Padding = 12f;

        private static readonly List<Renderer> RENDERERS = new();

        private Graphic _graphic;
        private Canvas _canvas;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _canvas = GetComponentInParent<Canvas>();
            _rectTransform = (RectTransform) transform;
        }

        private void LateUpdate()
        {
            var target = _Controller.Target;
            var visible = target != null && TryGetScreenRect(target, out var screenRect) && Place(screenRect);
            _graphic.enabled = visible;
        }

        private bool Place(Rect screenRect)
        {
            var parent = (RectTransform) _rectTransform.parent;
            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenRect.min, eventCamera, out var min) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenRect.max, eventCamera, out var max))
                return false;

            _rectTransform.localPosition = (min + max) * 0.5f;
            _rectTransform.sizeDelta = max - min + Vector2.one * (_Padding * 2f);
            return true;
        }

        private bool TryGetScreenRect(ScreenInteractable target, out Rect screenRect)
        {
            screenRect = default;
            if (!TryGetBounds(target, out var bounds))
                return false;

            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            for (var i = 0; i < 8; i++)
            {
                var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                var point = _Controller.Camera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
                // Behind the camera, the projection flips
                if (point.z <= 0f)
                    return false;

                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        // Mesh renderers only, particles and canvases would blow up the frame
        private static bool TryGetBounds(ScreenInteractable target, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            target.GetComponentsInChildren(RENDERERS);
            foreach (var renderer in RENDERERS)
            {
                if (!renderer.enabled || renderer is not (MeshRenderer or SkinnedMeshRenderer))
                    continue;

                if (found)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;
                found = true;
            }
            return found;
        }
    }
}
