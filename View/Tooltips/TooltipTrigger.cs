// Author: Michal Petr
// Created: 09.03.2026

using System;
using EDIVE.View.ViewTree;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EDIVE.View.Tooltips
{
    // Shows a tooltip from the TooltipManager above in the view tree, only while focused
    [RequireComponent(typeof(Graphic))]
    public class TooltipTrigger : AViewBehaviour, IPointerEnterHandler, IPointerExitHandler, IVisualPresetReceiver
    {
        [SerializeField]
        private VisualPreset _DefaultPreset;

        [SerializeField]
        private TooltipPlacement _Placement;

        [Tooltip("Hover delay for mouse and rays, touch shows right away")]
        [SerializeField]
        private float _ShowDelay = 0.35f;

        [Tooltip("Touch has no hover, keeps the tooltip up after the finger lifts")]
        [SerializeField]
        private float _TouchHideDelay = 1.5f;

        private Graphic _graphic;
        private TooltipManager _tooltipManager;

        private IDisposable _tooltipSubscription;
        private IDisposable _lingeringSubscription;
        private VisualPreset _currentVisualPreset;
        private float _showTime;
        private bool _showPending;

        protected override void Awake()
        {
            _graphic = GetComponent<Graphic>();
            base.Awake();
        }

        private void Update()
        {
            if (_showPending && Time.unscaledTime >= _showTime)
            {
                _showPending = false;
                ShowTooltip();
            }
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

        public VisualPreset Preset => _currentVisualPreset ?? _DefaultPreset;

        public void SetPreset(VisualPreset visualPreset)
        {
            // Records apply in order, so the default goes first as a fallback
            _currentVisualPreset = VisualPreset.Combine(_DefaultPreset, visualPreset);
            if (_tooltipSubscription != null)
                ShowTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_ShowDelay <= 0 || IsTouch(eventData))
            {
                ShowTooltip();
                return;
            }

            _showTime = Time.unscaledTime + _ShowDelay;
            _showPending = true;
        }

        private void ShowTooltip()
        {
            if (!FocusedInTree || !EnsureManager())
                return;

            _tooltipSubscription?.Dispose();

            _currentVisualPreset ??= _DefaultPreset;
            _tooltipSubscription = _tooltipManager.ShowTooltip(_currentVisualPreset, _graphic.rectTransform, _Placement);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_TouchHideDelay > 0 && _tooltipSubscription != null && _tooltipManager != null && IsTouch(eventData))
            {
                _lingeringSubscription = _tooltipSubscription;
                _tooltipSubscription = null;
                _tooltipManager.HideAfter(_lingeringSubscription, _TouchHideDelay);
                return;
            }

            HideTooltip();
        }

        private static bool IsTouch(PointerEventData eventData) => eventData is ExtendedPointerEventData {pointerType: UIPointerType.Touch};

        private void HideTooltip()
        {
            _showPending = false;
            _tooltipSubscription?.Dispose();
            _tooltipSubscription = null;
            if (_lingeringSubscription != null && _tooltipManager != null)
                _tooltipManager.HideNow(_lingeringSubscription);
            _lingeringSubscription = null;
        }
    }
}
