// Author: František Holubec
// Created: 20.06.2025

using EDIVE.Core;
using EDIVE.Networking.Players;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.Avatars.Networking
{
    public class NetworkAvatarSelector : AAvatarSelector
    {
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
            if (Definition == null)
                return;

            var localPlayer = AppCore.Services.Get<NetworkPlayerManager>().LocalPlayer;
            if (localPlayer == null)
            {
                Debug.LogWarning("Local player not found, cannot set avatar");
                return;
            }

            if (localPlayer.TryGetComponent<NetworkAvatarPlayerController>(out var playerController))
                playerController.SelectAvatar(Definition);
        }
    }
}
