// Author: Michal Petr
// Created: 22.09.2026

using EDIVE.StateHandling.ToggleStates;
using EDIVE.UIElements.Layout;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.ActionWheel
{
    public abstract class AActionWheelWedge : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private RadialLayoutElement _LayoutElement;
        
        [SerializeField]
        private AToggleState _HoveredVisual;
        
        public bool ContainsAngle(float angle)
        {
            var slice = _LayoutElement.CurrentSlice;
            return Mathf.Abs(Mathf.DeltaAngle(angle, slice.CenterAngle)) <= slice.Width * 0.5f;
        }

        public void SetHovered(bool hovered)
        {
            if (_HoveredVisual != null)
                _HoveredVisual.SetState(hovered);
        }
        
        public virtual void OnShow() { }
        public virtual void OnHide() { }

        public abstract void ExecuteActions();
    }
}
