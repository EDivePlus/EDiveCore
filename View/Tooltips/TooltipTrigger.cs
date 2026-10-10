// Author: Michal Petr
// Created: 09.03.2026

using System;
using EDIVE.View.ViewTree;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.View.Tooltips
{
    // Shows a tooltip from the TooltipManager above in the view tree, only while focused
    [RequireComponent(typeof(Graphic))]
    public class TooltipTrigger : AViewBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private VisualPreset _DefaultPreset;
        
        [SerializeField]
        private TooltipPlacement _Placement;
        
        private Graphic _graphic;
        private TooltipManager _tooltipManager;
        
        private IDisposable _tooltipSubscription;
        private VisualPreset _currentVisualPreset; 

        protected override void Awake()
        {
            _graphic = GetComponent<Graphic>();
            base.Awake();
        }

        // Pointer exit never comes once disabled, the tooltip would stay up
        protected override void OnDisable()
        {
            HideTooltip();
            base.OnDisable();
        }

        protected override void OnAttached(ViewGroup parent) => _tooltipManager = null;

        protected override void OnViewStateChanged(ViewState from, ViewState to)
        {
            if (!FocusedInTree)
                HideTooltip();
        }

        private bool EnsureManager()
        {
            if (_tooltipManager != null)
                return true;

            _tooltipManager = this.FindInViewTree<TooltipManager>();
            if (_tooltipManager == null)
            {
                Debug.LogError("[TooltipTrigger] No TooltipManager in the view tree above.", this);
                enabled = false;
                return false;
            }
            return true;
        }

        public void SetPreset(VisualPreset visualPreset)
        {
            _currentVisualPreset = VisualPreset.Combine(visualPreset, _DefaultPreset);
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!FocusedInTree || !EnsureManager())
                return;

            _tooltipSubscription?.Dispose();

            _currentVisualPreset ??= _DefaultPreset;
            _tooltipSubscription = _tooltipManager.ShowTooltip(_currentVisualPreset, _graphic.rectTransform, _Placement);
        }

        public void OnPointerExit(PointerEventData eventData) => HideTooltip();

        private void HideTooltip()
        {
            _tooltipSubscription?.Dispose();
            _tooltipSubscription = null;
        }
    }
}
