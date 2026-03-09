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
        private TooltipPlacement _Placement;
        
        private Graphic _graphic;
        private TooltipManager _tooltipManager;
        
        private IDisposable _tooltipSubscription;
        private VisualPreset _visualPreset; 

        private void Awake()
        {
            _tooltipManager = GetComponentInParent<TooltipManagerProvider>().TooltipManager;
            _graphic = GetComponent<Graphic>();
        }
        
        public void SetPreset(VisualPreset visualPreset)
        {
            _visualPreset = visualPreset;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        { 
            _tooltipSubscription = _tooltipManager.ShowTooltip(_visualPreset, _graphic.rectTransform, _Placement);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltipSubscription?.Dispose();
            _tooltipSubscription = null;
        }
    }
}
