// Author: Michal Petr
// Created: 21.05.2026

using System;
using EDIVE.Core;
using EDIVE.Input.Controls;
using EDIVE.Networking.Players;
using EDIVE.Networking.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Avatars.Networking
{
    [Serializable]
    public class TeleportNetworkPlayerDisplayComponent : ANetworkPlayerDisplayComponent
    {
        [SerializeField]
        private Button _TeleportToButton;
        
        private NetworkAvatarPlayerController _avatarPlayerController;
        
        public override void InitializeForPlayer(NetworkPlayerController playerController)
        {
            base.InitializeForPlayer(playerController);
            _avatarPlayerController = playerController.GetComponent<NetworkAvatarPlayerController>();
            
            var canTeleport = CanTeleportToPlayer(playerController);
            if (_TeleportToButton) _TeleportToButton.interactable = canTeleport;
        }

        public override void RegisterListeners()
        {
            if (_TeleportToButton) _TeleportToButton.onClick.AddListener(TeleportToPlayer);
        }

        public override void UnregisterListeners()
        {
            if (_TeleportToButton) _TeleportToButton.onClick.RemoveListener(TeleportToPlayer);
        }

        public bool CanTeleportToPlayer(NetworkPlayerController player)
        {
            return player != null && player != AppCore.Services.Get<NetworkPlayerManager>().LocalPlayer;
        }
        
        private void TeleportToPlayer()
        {
            if (!CanTeleportToPlayer(PlayerController))
                return;

            if (!AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                return;

            var targetTransform = _avatarPlayerController.GetWorldPoseTransform();
            controlsManager.RequestTeleport(targetTransform.position, targetTransform.rotation);
        }
    }
}
