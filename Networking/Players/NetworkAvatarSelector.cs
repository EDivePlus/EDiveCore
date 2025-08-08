// Author: František Holubec
// Created: 20.06.2025

using EDIVE.Avatars;
using FishNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.Networking.Players
{
    public class NetworkAvatarSelector : MonoBehaviour
    {
        [SerializeField]
        private AvatarDefinition _Definition;

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
            SelectAvatar();
        }

        private void OnInteractableActivated(ActivateEventArgs arg0)
        {
            SelectAvatar();
        }

        private void SelectAvatar()
        {
            if (_Definition == null)
                return;
            
            var localPlayer = InstanceFinder.ClientManager.Connection.FirstObject;
            if (localPlayer == null)
            {
                Debug.LogWarning("Local player not found, cannot set avatar");
                return;
            }

            if (localPlayer.TryGetComponent<NetworkPlayerController>(out var playerController))
                playerController.SetAvatar(_Definition);
        }
    }
}
