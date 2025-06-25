// Author: František Holubec
// Created: 20.06.2025

using EDIVE.Avatars;
using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UVRN.Player;

namespace EDIVE.MirrorNetworking.Players
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
            if (_XRInteractable)
                _XRInteractable.activated.AddListener(OnInteractableActivated);

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

            var localPlayer = AppCore.Services.Get<NetworkPlayerManager>().LocalPlayer;
            if (localPlayer == null)
            {
                Debug.LogWarning("Local player not found, cannot set avatar");
                return;
            }

            localPlayer.SetAvatar(_Definition);
        }
    }
}
