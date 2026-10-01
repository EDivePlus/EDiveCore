// Author: Michal Petr
// Created: 22.09.2026

using EDIVE.Conditions;
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
        [PropertySpace]
        private AToggleState _HoveredVisual;
        
        [SerializeField]
        private AToggleState _LockedVisual;

        [PropertySpace]
        [SerializeReference]
        private ICondition _VisibleCondition;
        
        [SerializeReference]
        private ICondition _UnlockedCondition;

        public bool IsVisible => _VisibleCondition?.Evaluate() ?? true;
        public bool IsUnlocked => _UnlockedCondition?.Evaluate() ?? true;
        public bool CanExecute => IsVisible && IsUnlocked;

        public bool ContainsAngle(float angle)
        {
            var slice = _LayoutElement.CurrentSlice;
            return Mathf.Abs(Mathf.DeltaAngle(angle, slice.CenterAngle)) <= slice.Width * 0.5f;
        }

        public void SetHovered(bool hovered)
        {
            if (CanExecute && _HoveredVisual != null)
                _HoveredVisual.SetState(hovered);
        }

        public virtual void OnShow()
        {
            if (_LockedVisual != null)
                _LockedVisual.SetState(!IsUnlocked);
        }
        public virtual void OnHide() { }

        public abstract void ExecuteActions();
    }
}
