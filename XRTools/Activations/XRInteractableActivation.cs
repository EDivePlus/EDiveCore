// Author: František Holubec
// Created: 04.09.2025

using System;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.XRTools.Activations
{
    [Serializable]
    public class XRInteractableActivation : AWrapperActivation
    {
        [SerializeField]
        private XRBaseInteractable _Interactable;

        [SerializeField]
        [Tooltip("Select events fire the activation only when the interactor's interaction layers overlap this mask. Set to Nothing to disable.")]
        private InteractionLayerMask _SelectMask = -1;

        [SerializeField]
        [Tooltip("Activate events fire the activation only when the interactor's interaction layers overlap this mask. Set to Nothing to disable.")]
        private InteractionLayerMask _ActivateMask = -1;

        protected override void StartListening()
        {
            if (_Interactable == null) return;
            _Interactable.selectEntered.AddListener(OnSelectEntered);
            _Interactable.activated.AddListener(OnActivated);
        }

        protected override void StopListening()
        {
            if (_Interactable == null) return;
            _Interactable.selectEntered.RemoveListener(OnSelectEntered);
            _Interactable.activated.RemoveListener(OnActivated);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (Matches(args.interactorObject, _SelectMask))
                InvokeListeners();
        }

        private void OnActivated(ActivateEventArgs args)
        {
            if (Matches(args.interactorObject, _ActivateMask))
                InvokeListeners();
        }

        private static bool Matches(IXRInteractor interactor, InteractionLayerMask mask)
        {
            if (interactor == null || mask.value == 0) return false;
            return (interactor.interactionLayers.value & mask.value) != 0;
        }

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (_Interactable == null && property.SerializationRoot.ValueEntry.WeakSmartValue is MonoBehaviour monoBehaviour && monoBehaviour.TryGetComponent<XRBaseInteractable>(out var interactable))
            {
                _Interactable = interactable;
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
