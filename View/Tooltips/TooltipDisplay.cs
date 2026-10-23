// Author: Michal Petr
// Created: 09.03.2026

using EDIVE.StateHandling.MultiStates;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EDIVE.View.Tooltips
{
    public class TooltipDisplay : MonoBehaviour
    {
        private static readonly Vector3[] CORNERS = new Vector3[4];

        [SerializeField]
        [FormerlySerializedAs("_Switcher")]
        private VisualSwitcher _VisualSwitcher;

        [SerializeField]
        private RectTransform _Container;
        
        [SerializeField]
        [ValidateMultiState(typeof(TooltipPlacement))]
        private AMultiState _PlacementState;
        
        public void ShowTooltip(VisualPreset preset, RectTransform target, Canvas canvas, TooltipPlacement preferredPlacement, bool keepInsideCanvas = true)
        {
            gameObject.SetActive(true);
            _VisualSwitcher.Apply(preset);
            UpdatePlacement(target, canvas, preferredPlacement, keepInsideCanvas);
        }
        
        public void HideTooltip()
        {
            gameObject.SetActive(false);
        }
        
        private void UpdatePlacement(RectTransform target, Canvas canvas, TooltipPlacement preferredPlacement, bool keepInsideCanvas)
        {
            _PlacementState.SetState(preferredPlacement);

            transform.localPosition = GetEdgePosition(target, preferredPlacement);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_Container);

            if (keepInsideCanvas)
                ResolvePlacement(target, (RectTransform) canvas.transform, preferredPlacement);
        }

        // Flips to the side with room when the tooltip leaves the canvas rect
        private void ResolvePlacement(RectTransform target, RectTransform canvasRect, TooltipPlacement preferredPlacement)
        {
            _Container.GetWorldCorners(CORNERS);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var corner in CORNERS)
            {
                var screenPoint = canvasRect.InverseTransformPoint(corner);
                if (screenPoint.x < minX) minX = screenPoint.x;
                if (screenPoint.x > maxX) maxX = screenPoint.x;
                if (screenPoint.y < minY) minY = screenPoint.y;
                if (screenPoint.y > maxY) maxY = screenPoint.y;
            }
            
            var safeArea = canvasRect.rect;
            var leftOverflow = Mathf.Max(safeArea.xMin - minX, 0f);
            var rightOverflow = Mathf.Max(maxX - safeArea.xMax, 0f);
            var bottomOverflow = Mathf.Max(safeArea.yMin - minY, 0f);
            var topOverflow = Mathf.Max(maxY - safeArea.yMax, 0f);
            
            if (leftOverflow == 0f && rightOverflow == 0f && topOverflow == 0f && bottomOverflow == 0f)
                return;
            
            var applyTop = (topOverflow == 0f && bottomOverflow == 0f)
                ? !preferredPlacement.IsBottom() 
                : topOverflow <= bottomOverflow;
            
            TooltipPlacement newPlacement;

            if (leftOverflow > rightOverflow)
                newPlacement = applyTop ? TooltipPlacement.TopRight : TooltipPlacement.BottomRight;
            else if (rightOverflow > leftOverflow)
                newPlacement = applyTop ? TooltipPlacement.TopLeft : TooltipPlacement.BottomLeft;
            else
                newPlacement = applyTop ? TooltipPlacement.TopCenter : TooltipPlacement.BottomCenter;

            if (newPlacement.Equals(preferredPlacement)) 
                return;
            
            _PlacementState.SetState(newPlacement);
            transform.localPosition = GetEdgePosition(target, newPlacement);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_Container);
        }
        
        // Target edge in the parent's space, it's what localPosition is relative to
        private Vector3 GetEdgePosition(RectTransform target, TooltipPlacement placement)
        {
            var targetRect = target.rect;
            var localPoint = targetRect.center;

            localPoint.y = placement.IsBottom() ? targetRect.yMin : targetRect.yMax;

            var worldPoint = target.TransformPoint(localPoint);
            return transform.parent.InverseTransformPoint(worldPoint);
        }
    }
}