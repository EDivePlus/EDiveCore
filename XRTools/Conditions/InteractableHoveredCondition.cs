// Author: František Holubec
// Created: 16.06.2026

using System;
using EDIVE.Conditions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.Conditions
{
    [Serializable]
    public class InteractableHoveredCondition : ABoolCondition
    {
        [SerializeField]
        private XRBaseInteractable _Interactable;
        
        protected override bool GetValue() => _Interactable != null && _Interactable.isHovered;
        
        public override void InitializeObserving()
        {
            if (_Interactable != null)
            {
                _Interactable.hoverEntered.AddListener(OnHoverEntered);
                _Interactable.hoverExited.AddListener(OnHoverExited);
            }
        }

        public override void TerminateObserving()
        {
            if (_Interactable != null)
            {
                _Interactable.hoverEntered.RemoveListener(OnHoverEntered);
                _Interactable.hoverExited.RemoveListener(OnHoverExited);
            }
        }

        private void OnHoverExited(HoverExitEventArgs arg0)
        {
            InvokeStateChanged();
        }

        private void OnHoverEntered(HoverEnterEventArgs arg0)
        {
            InvokeStateChanged();
        }
    }
}
