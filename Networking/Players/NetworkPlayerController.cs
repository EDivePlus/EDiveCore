
// Author: František Holubec
// Created: 23.04.2025

using EDIVE.Core;
using EDIVE.Networking.UI;
using EDIVE.StateHandling.ToggleStates;
using PurrNet;
using UnityEngine;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private BillboardNameTag _NameTag;

        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
        {
            if (asServer) return;
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isOwner);
        }

        protected override void OnSpawned(bool asServer)
        {
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this, asServer);
        }

        protected override void OnDespawned(bool asServer)
        {
            if (AppCore.Services.TryGet<NetworkPlayerManager>(out var playerManager))
            {
                playerManager.UnregisterPlayer(this, asServer);
            }
        }
    }
}
