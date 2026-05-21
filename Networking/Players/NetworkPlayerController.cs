// Author: František Holubec
// Created: 23.04.2025

using System;
using System.Threading;
using EDIVE.Core;
using EDIVE.Networking.UI;
using EDIVE.ServiceHub;
using EDIVE.StateHandling.ToggleStates;
using PurrNet;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private BillboardNameTag _NameTag;

        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        private readonly SyncVar<NetworkUserInfo> _authUserInfo = new(ownerAuth: true);
        private readonly SyncVar<int> _ping = new();

        private CancellationTokenSource _cts;

        [ShowInInspector, ReadOnly]
        public int Ping => _ping.value;
        
        public NetworkUserInfo AuthUserInfo => _authUserInfo.value;

        public event Action<NetworkUserInfo> AuthUserInfoChanged
        {
            add => _authUserInfo.onChanged += value;
            remove => _authUserInfo.onChanged -= value;
        }
        public event Action<int> PingChanged
        {
            add => _ping.onChanged += value;
            remove => _ping.onChanged -= value;
        }

        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
        {
            if (asServer) return;
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isOwner);
        }

        protected override void OnSpawned(bool asServer)
        {
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this, asServer);
            
            if (AppCore.Services.TryGet<ServiceHubManager>(out var serviceHubManager) 
                && serviceHubManager.ClientAuth != null 
                && serviceHubManager.ClientAuth.TryGetAuthUserInfo(out var authUserInfo))
            {
                _authUserInfo.value = NetworkUserInfo.FromAuthUserInfo(authUserInfo);
            }
            else
            {
                _authUserInfo.value = NetworkUserInfo.CreateAnonymous();
            }

            if (asServer)
            {
                if (_cts == null || _cts.IsCancellationRequested)
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            if (AppCore.Services.TryGet<NetworkPlayerManager>(out var playerManager))
            {
                playerManager.UnregisterPlayer(this, asServer);
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void LateUpdate()
        {
            if (!isOwner || !isSpawned) return;
            if (!AppCore.Services.TryGet<MasterNetworkManager>(out var masterNetworkManager)) return;
            if (masterNetworkManager.StatisticsManager == null) return;
            
            ServerSetPlayerPing(masterNetworkManager.StatisticsManager.ping);
        }
        
        [ServerRpc]
        private void ServerSetPlayerPing(int ping)
        {
            _ping.value = ping;
        }
    }
}
