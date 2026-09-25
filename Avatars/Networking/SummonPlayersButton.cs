// Author: František Holubec
// Created: 22.01.2026

using EDIVE.Core;
using EDIVE.Networking.Players;
using EDIVE.Utils.Activations;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public class SummonPlayersButton : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;
        
        private void Awake()
        {
            _Activation?.RegisterActivationListener(OnActivated);
        }
        
        private void OnDestroy()
        {
            _Activation?.UnregisterActivationListener(OnActivated);
        }

        private void OnActivated()
        {
            if (AppCore.Services.TryGet<NetworkPlayerManager>(out var networkPlayerManager) &&
                networkPlayerManager.LocalPlayer != null &&
                networkPlayerManager.LocalPlayer.TryGetComponent<NetworkAvatarPlayerController>(out var controller))
            {
                controller.SummonPlayersToMe();
            }
        }
    }
}
