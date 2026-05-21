// Author: František Holubec
// Created: 23.04.2025

using System;
using System.Threading;
using EDIVE.Core;
using EDIVE.Networking.UI;
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
        
        private const float PING_TIMEOUT = 5f;
        private const float PING_TIMEOUT_CHECK_INTERVAL = 1f;
        private const int PING_TIMEOUT_VALUE = 5000;

        private readonly SyncVar<int> _ping = new();

        private CancellationTokenSource _cts;
        private float _lastPingUpdateServerTime;

        [ShowInInspector, ReadOnly]
        public int Ping => _ping.value;

        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
        {
            if (asServer) return;
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isOwner);
        }

        protected override void OnSpawned(bool asServer)
        {
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this, asServer);

            if (asServer)
            {
                if (_cts == null || _cts.IsCancellationRequested)
                    _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                
                _ping.onChanged += OnServerPingChanged;
                Observable.Interval(TimeSpan.FromSeconds(PING_TIMEOUT_CHECK_INTERVAL))
                    .Subscribe(_ => CheckPingTimeout())
                    .RegisterTo(_cts.Token);
            }
        }

        private void OnServerPingChanged(int value)
        {
            _lastPingUpdateServerTime = UnityEngine.Time.time;
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

        [Server]
        private void CheckPingTimeout()
        {
            if (_ping.value == PING_TIMEOUT_VALUE) return;
            if (UnityEngine.Time.time - _lastPingUpdateServerTime <= PING_TIMEOUT) return;

            _ping.value = PING_TIMEOUT_VALUE;
        }
        
        [ServerRpc]
        private void ServerSetPlayerPing(int ping)
        {
            _ping.value = ping;
        }
    }
}
