// Author: František Holubec
// Created: 20.06.2025

using EDIVE.Core;
using EDIVE.Networking.Players;
using EDIVE.StateHandling.ToggleStates;
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

        // On while this is the local player's avatar
        [SerializeField]
        private AToggleState _CurrentState;

        private NetworkPlayerManager _playerManager;
        private NetworkAvatarPlayerController _localController;

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

        private void OnEnable()
        {
            if (AppCore.Services.TryGet(out _playerManager))
            {
                _playerManager.PlayerRegistered += OnPlayerRegistered;
                BindLocalPlayer(_playerManager.LocalPlayer);
            }
            RefreshCurrent();
        }

        private void OnDisable()
        {
            if (_playerManager != null)
                _playerManager.PlayerRegistered -= OnPlayerRegistered;
            _playerManager = null;
            BindLocalPlayer(null);
        }

        public override void SetDefinition(AvatarDefinition definition)
        {
            base.SetDefinition(definition);
            RefreshCurrent();
        }

        private void OnPlayerRegistered(NetworkPlayerController player)
        {
            if (_playerManager != null)
                BindLocalPlayer(_playerManager.LocalPlayer);
        }

        private void BindLocalPlayer(NetworkPlayerController player)
        {
            if (_localController != null)
                _localController.AvatarDefinitionChanged -= OnAvatarDefinitionChanged;
            _localController = player != null ? player.GetComponent<NetworkAvatarPlayerController>() : null;
            if (_localController != null)
                _localController.AvatarDefinitionChanged += OnAvatarDefinitionChanged;
            RefreshCurrent();
        }

        private void OnAvatarDefinitionChanged(AvatarDefinition definition) => RefreshCurrent();

        private void RefreshCurrent()
        {
            if (_CurrentState != null)
                _CurrentState.SetState(Definition != null && _localController != null && _localController.AvatarDefinition == Definition);
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

            if (!AppCore.Services.TryGet<NetworkPlayerManager>(out var playerManager))
            {
                Debug.LogWarning("Network player manager not found.");
                return;
            }
            
            var localPlayer = playerManager.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.LogWarning("Local player not found, cannot set avatar");
                return;
            }

            if (localPlayer.TryGetComponent<NetworkAvatarPlayerController>(out var playerController))
                playerController.SetAvatar(Definition);
        }
    }
}
