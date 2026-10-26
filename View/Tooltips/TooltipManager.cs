// Author: Michal Petr
// Created: 09.03.2026

using System;
using System.Collections.Generic;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EDIVE.View.Tooltips
{
    public class TooltipManager : MonoBehaviour
    {
        [SerializeField]
        private List<TooltipDisplay> _Displays;
        
        [SerializeField]
        private Canvas _ParentCanvas;
        
        [SerializeField]
        private RectTransform _TooltipRoot;

        [Tooltip("Flip tooltips that leave the parent canvas rect. Off for small world space canvases like cards.")]
        [SerializeField]
        private bool _KeepInsideCanvas = true;
        
        private Queue<TooltipDisplay> AvailableDisplays { get; set; }

        private IDisposable _lingeringTooltip;
        private float _lingerEndTime;

        private void Awake()
        {
            if (_ParentCanvas == null)
                _ParentCanvas = GetComponentInParent<Canvas>();
            
            if (_TooltipRoot == null)
                _TooltipRoot = transform as RectTransform;
            
            AvailableDisplays = new Queue<TooltipDisplay>(_Displays.Count);
            foreach (var display in _Displays)
            {
                display.gameObject.SetActive(false);
                display.transform.SetParent(_TooltipRoot, false);
                AvailableDisplays.Enqueue(display);
            }
        }
        
        private void Update()
        {
            // A new tap anywhere hides it too
            if (_lingeringTooltip != null && (Time.unscaledTime >= _lingerEndTime || IsTouchStarted()))
                ReleaseLingering();
        }

        private static bool IsTouchStarted() => Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        // Keeps a shown tooltip up for a while, the next shown tooltip replaces it
        public void HideAfter(IDisposable tooltip, float delay)
        {
            ReleaseLingering();
            _lingeringTooltip = tooltip;
            _lingerEndTime = Time.unscaledTime + delay;
        }

        public void HideNow(IDisposable tooltip)
        {
            if (ReferenceEquals(_lingeringTooltip, tooltip))
                ReleaseLingering();
        }

        private void ReleaseLingering()
        {
            _lingeringTooltip?.Dispose();
            _lingeringTooltip = null;
        }

        public IDisposable ShowTooltip(VisualPreset preset, RectTransform target, TooltipPlacement preferredPlacement)
        {
            ReleaseLingering();
            if (!AvailableDisplays.TryDequeue(out var display))
            {
                Debug.LogWarning("No available tooltip displays in the pool!");
                return null;
            }
            
            display.transform.SetAsLastSibling();
            display.ShowTooltip(preset, target, _ParentCanvas, preferredPlacement, _KeepInsideCanvas);
            
            return new TooltipHandle(this, display);
        }
        
        private void ReleaseTooltip(TooltipDisplay display)
        {
            if (display == null || !display.gameObject.activeSelf) 
                return; 
            
            display.HideTooltip();
            display.gameObject.SetActive(false);
            AvailableDisplays.Enqueue(display);
        }
        
        private readonly struct TooltipHandle : IDisposable
        {
            private readonly TooltipManager _manager;
            private readonly TooltipDisplay _display;

            public TooltipHandle(TooltipManager manager, TooltipDisplay display)
            {
                _manager = manager;
                _display = display;
            }

            public void Dispose()
            {
                if (_manager != null) _manager.ReleaseTooltip(_display);
            }
        }
    }
}
