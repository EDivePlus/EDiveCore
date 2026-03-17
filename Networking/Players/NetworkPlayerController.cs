// Author: František Holubec
// Created: 23.04.2025

using EDIVE.Avatars;
using EDIVE.Core;
using EDIVE.Networking.UI;
using EDIVE.Networking.Utils;
using EDIVE.StateHandling.ToggleStates;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using FishNet.Connection;
using EDIVE.XRTools.Controls;
using FishNet;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private BillboardNameTag _NameTag;
        
        [SerializeField]
        private AToggleState _LocalPlayerToggle;


        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            if(_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(IsOwner);
        }
        
        public override void OnStartClient()
        {
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this);
        }

        public override void OnStartServer()
        {
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this);
        }

        public override void OnStopNetwork()
        {
            if (AppCore.Services.TryGet<NetworkPlayerManager>(out var playerManager))
            {
                playerManager.UnregisterPlayer(this);
            }
        }
    }
}
