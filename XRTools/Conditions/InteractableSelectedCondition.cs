// Author: František Holubec
// Created: 18.06.2026

using System;
using EDIVE.Conditions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.Conditions
{
    [Serializable]
    public class InteractableSelectedCondition : ABoolCondition
    {
        [SerializeField]
        private XRBaseInteractable _Interactable;
        
        protected override bool GetValue() => _Interactable != null && _Interactable.isHovered;
        
        public override void InitializeObserving()
        {
            if (_Interactable != null)
            {
                _Interactable.selectEntered.AddListener(OnSelectEntered);
                _Interactable.selectExited.AddListener(OnSelectExited);
            }
        }

        public override void TerminateObserving()
        {
            if (_Interactable != null)
            {
                _Interactable.selectEntered.AddListener(OnSelectEntered);
                _Interactable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnSelectExited(SelectExitEventArgs arg0)
        {
            InvokeStateChanged();
        }

        private void OnSelectEntered(SelectEnterEventArgs arg0)
        {
            InvokeStateChanged();
        }
    }
}
