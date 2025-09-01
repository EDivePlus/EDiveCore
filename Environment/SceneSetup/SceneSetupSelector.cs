// Author: František Holubec
// Created: 28.08.2025

using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupSelector : MonoBehaviour
    {
        [SerializeField]
        private SceneSetupDefinition _Definition;

        [SerializeField]
        private XRBaseInteractable _XRInteractable;

        [SerializeField]
        private Button _Button;
        
        private void Awake()
        {
            if (_XRInteractable == null && TryGetComponent(out XRBaseInteractable xrInteractable))
                _XRInteractable = xrInteractable;

            if (_XRInteractable)
                _XRInteractable.activated.AddListener(OnInteractableActivated);

            if (_Button == null && TryGetComponent(out Button button))
                _Button = button;

            if (_Button)
                _Button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            ChangeSceneContext();
        }

        private void OnInteractableActivated(ActivateEventArgs arg0)
        {
            ChangeSceneContext();
        }

        private void ChangeSceneContext()
        {
            if (_Definition == null)
                return;

            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneContextManager))
                sceneContextManager.SetCurrentContext(_Definition);
        }
    }
}
