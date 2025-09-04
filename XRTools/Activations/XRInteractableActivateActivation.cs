// Author: František Holubec
// Created: 04.09.2025

using System;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.XRTools.Activations
{
    [Serializable]
    public class XRInteractableActivateActivation : AWrapperActivation
    {
        [SerializeField] 
        private XRBaseInteractable _Interactable;

        protected override void StartListening()
        {
            if (_Interactable != null)
                _Interactable.activated.AddListener(OnActivated);
        }

        protected override void StopListening()
        {
            if (_Interactable != null)
                _Interactable.activated.RemoveListener(OnActivated);
        }

        private void OnActivated(ActivateEventArgs args) => InvokeListeners();
        
#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (property.SerializationRoot.ValueEntry.WeakSmartValue is MonoBehaviour monoBehaviour && monoBehaviour.TryGetComponent<XRBaseInteractable>(out var interactable))
            {
                _Interactable = interactable;
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
