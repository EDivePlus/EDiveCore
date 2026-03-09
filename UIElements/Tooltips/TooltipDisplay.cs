// Author: Michal Petr
// Created: 09.03.2026

using EDIVE.StateHandling.MultiStates;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.Tooltips
{
    public class TooltipDisplay : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Switcher;
        
        [SerializeField]
        [ValidateMultiState(typeof(TooltipPlacement))]
        private AMultiState _PlacementState;
        
        public void ShowTooltip(VisualPreset preset, Vector2 position, Canvas canvas, TooltipPlacement preferredPlacement)
        {
            _Switcher.Apply(preset);
            UpdatePlacement(position, canvas, preferredPlacement);
            gameObject.SetActive(true);
        }
        
        public void HideTooltip()
        {
            gameObject.SetActive(false);
        }
        
        private void UpdatePlacement(Vector2 position, Canvas canvas, TooltipPlacement preferredPlacement)
        {
            gameObject.SetActive(true);
            var rectTr = (RectTransform) transform;
            var canvasRect = (RectTransform) canvas.transform;

            _PlacementState.SetState(preferredPlacement);
            rectTr.anchoredPosition = position;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTr);

            var objectCorners = new Vector3[4];
            rectTr.GetWorldCorners(objectCorners);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var corner in objectCorners)
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

            _PlacementState.SetState(newPlacement);
        }
    }
}
