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
    public class InteractableActivatedCondition : ABoolCondition
    {
        [SerializeField]
        private XRBaseInteractable _Interactable;
        
        protected override bool GetValue() => _Interactable != null && _Interactable.isHovered;
        
        public override void InitializeObserving()
        {
            if (_Interactable != null)
            {
                _Interactable.activated.AddListener(OnActivated);
                _Interactable.deactivated.AddListener(OnDeactivated);
            }
        }

        public override void TerminateObserving()
        {
            if (_Interactable != null)
            {
                _Interactable.activated.AddListener(OnActivated);
                _Interactable.deactivated.AddListener(OnDeactivated);
            }
        }

        private void OnDeactivated(DeactivateEventArgs deactivateEventArgs)
        {
            InvokeStateChanged();
        }

        private void OnActivated(ActivateEventArgs activateEventArgs)
        {
            InvokeStateChanged();
        }
    }
}
