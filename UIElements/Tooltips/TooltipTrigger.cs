// Author: Michal Petr
// Created: 09.03.2026

using System;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.UIElements.Tooltips
{
    [RequireComponent(typeof(Graphic))]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private VisualPreset _DefaultPreset;
        
        [SerializeField]
        private TooltipPlacement _Placement;
        
        private Graphic _graphic;
        private TooltipManager _tooltipManager;
        
        private IDisposable _tooltipSubscription;
        private VisualPreset _currentVisualPreset; 

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        private bool EnsureManager()
        {
            if (_tooltipManager != null)
                return true;

            var provider = GetComponentInParent<TooltipManagerProvider>(true);
            if (provider == null)
            {
                Debug.LogError("TooltipTrigger requires a TooltipManagerProvider in its parent hierarchy.", this);
                enabled = false;
                return false;
            }

            _tooltipManager = provider.TooltipManager;
            return true;
        }

        private void OnDestroy()
        {
            _tooltipSubscription?.Dispose();
        }

        public void SetPreset(VisualPreset visualPreset)
        {
            _currentVisualPreset = VisualPreset.Combine(visualPreset, _DefaultPreset);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!EnsureManager())
                return;

            _tooltipSubscription?.Dispose();

            _currentVisualPreset ??= _DefaultPreset;
            _tooltipSubscription = _tooltipManager.ShowTooltip(_currentVisualPreset, _graphic.rectTransform, _Placement);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltipSubscription?.Dispose();
            _tooltipSubscription = null;
        }
    }
}
