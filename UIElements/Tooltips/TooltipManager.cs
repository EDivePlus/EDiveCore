// Author: Michal Petr
// Created: 09.03.2026

using System;
using System.Collections.Generic;
using EDIVE.VisualPresets.Presets;
using UnityEngine;

namespace EDIVE.UIElements.Tooltips
{
    public class TooltipManager : MonoBehaviour
    {
        [SerializeField]
        private List<TooltipDisplay> _Displays;
        
        [SerializeField]
        private Canvas _ParentCanvas;
        
        private Queue<TooltipDisplay> AvailableDisplays { get; set; }

        private void Awake()
        {
            AvailableDisplays = new Queue<TooltipDisplay>(_Displays.Count);
            
            foreach (var display in _Displays)
            {
                display.gameObject.SetActive(false);
                display.transform.SetParent(_ParentCanvas.transform, false);
                AvailableDisplays.Enqueue(display);
            }
        }
        
        public IDisposable ShowTooltip(VisualPreset preset, RectTransform rect, TooltipPlacement preferredPlacement)
        {
            if (!AvailableDisplays.TryDequeue(out var display))
            {
                Debug.LogWarning("No available tooltip displays in the pool!");
                return null;
            }
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_ParentCanvas.transform as RectTransform, rect.position, _ParentCanvas.worldCamera, out var position);
            
            display.transform.SetAsLastSibling();
            display.ShowTooltip(preset, position, _ParentCanvas, preferredPlacement);
            
            return new TooltipHandle(this, display);
        }
        
        private void ReleaseTooltip(TooltipDisplay display)
        {
            if (display == null || !display.gameObject.activeSelf) 
                return; 
            
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
