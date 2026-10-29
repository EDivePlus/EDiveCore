// Author: František Holubec
// Created: 23.04.2025

using System;
using EDIVE.Core;
using EDIVE.Input.Controls;
using EDIVE.Networking.ServerManagement;
using EDIVE.Networking.UI;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.Presence;
using EDIVE.StateHandling.ToggleStates;
using PurrNet;
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
        private readonly SyncVar<string> _device = new(ownerAuth: true);
        private readonly SyncVar<int> _ping = new();
        private readonly SyncVar<bool> _isHost = new();

        private readonly SyncVar<Vector3> _controlsPosition = new(ownerAuth: true);
        private readonly SyncVar<Quaternion> _controlsRotation = new(ownerAuth: true);
        
        [ShowInInspector, ReadOnly] 
        public int Ping => _ping.value;
        
        [ShowInInspector, ReadOnly] 
        public Vector3 ControlsPosition => _controlsPosition.value;
        
        [ShowInInspector, ReadOnly] 
        public Quaternion ControlsRotation => _controlsRotation.value;
        
        public NetworkUserInfo AuthUserInfo => _authUserInfo.value;

        // Player of the host, never true on a dedicated server
        public bool IsHost => _isHost.value;

        // headset, desktop or mobile
        public string Device => _device.value;
        
        private const float PING_SEND_INTERVAL = 1f;
        private float _nextPingSendTime;
        private int _lastSentPing = -1;
        
        private ControlsManager _controlsManager;
        private NetworkServerManager _serverManager;
        private ServerRecord _presenceServer;

        public event Action<NetworkUserInfo> AuthUserInfoChanged
        {
            add => _authUserInfo.onChanged += value;
            remove => _authUserInfo.onChanged -= value;
        }
        
        public event Action<string> DeviceChanged
        {
            add => _device.onChanged += value;
            remove => _device.onChanged -= value;
        }

        public event Action<int> PingChanged
        {
            add => _ping.onChanged += value;
            remove => _ping.onChanged -= value;
        }

        public event Action<bool> IsHostChanged
        {
            add => _isHost.onChanged += value;
            remove => _isHost.onChanged -= value;
        }

        private void Awake()
        {
            _controlsManager = AppCore.Services.Get<ControlsManager>();
        }

        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
        {
            if (asServer)
            {
                RefreshHost();
                return;
            }
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isOwner);
        }

        protected override void OnSpawned(bool asServer)
        {
            var playerManager = AppCore.Services.Get<NetworkPlayerManager>();
            playerManager.RegisterPlayer(this, asServer);

            _authUserInfo.onChanged += OnAuthUserInfoChanged;
            RefreshNameTag(_authUserInfo.value);

            if (asServer)
                RefreshHost();

            if (!asServer && isOwner)
            {
                if (AppCore.Services.TryGet<ServiceHubManager>(out var serviceHubManager)
                    && serviceHubManager.ClientAuth != null
                    && serviceHubManager.ClientAuth.TryGetAuthUserInfo(out var authUserInfo))
                {
                    _authUserInfo.value = NetworkUserInfo.FromAuthUserInfo(authUserInfo);
                }
                else
                {
                    _authUserInfo.value = NetworkUserInfo.CreateAnonymous(playerManager.GeneratePlayerName());
                }

                // Log in or linking while connected renames the player
                if (serviceHubManager != null && serviceHubManager.ClientAuth != null)
                    serviceHubManager.ClientAuth.UserInfoChanged += OnLocalUserInfoChanged;

                _device.value = PresenceService.DetectDevice();
                BindPresence();
            }

        }

        protected override void OnDespawned(bool asServer)
        {
            _authUserInfo.onChanged -= OnAuthUserInfoChanged;
            if (!asServer && AppCore.Services.TryGet<ServiceHubManager>(out var serviceHubManager) && serviceHubManager.ClientAuth != null)
                serviceHubManager.ClientAuth.UserInfoChanged -= OnLocalUserInfoChanged;
            if (!asServer && isOwner)
                UnbindPresence();

            if (AppCore.Services.TryGet<NetworkPlayerManager>(out var playerManager))
            {
                playerManager.UnregisterPlayer(this, asServer);
            }

        }

        private void Update()
        {
            if (!isOwner || !isSpawned || _controlsManager == null || _controlsManager.CurrentControls == null) return;
            _controlsPosition.value = _controlsManager.CurrentControls.Position;
            _controlsRotation.value = _controlsManager.CurrentControls.Rotation;
        }

        private void LateUpdate()
        {
            if (!isOwner || !isSpawned) return;
            if (!AppCore.Services.TryGet<MasterNetworkManager>(out var masterNetworkManager)) return;
            if (masterNetworkManager.StatisticsManager == null) return;
            // Once per second and only on change
            if (UnityEngine.Time.unscaledTime < _nextPingSendTime) return;
            _nextPingSendTime = UnityEngine.Time.unscaledTime + PING_SEND_INTERVAL;

            var ping = masterNetworkManager.StatisticsManager.ping;
            if (ping == _lastSentPing) return;
            _lastSentPing = ping;
            ServerSetPlayerPing(ping);
        }
        
        [ServerRpc]
        private void ServerSetPlayerPing(int ping)
        {
            _ping.value = ping;
        }

        private void RefreshHost()
        {
            _isHost.value = isHost && owner.HasValue && owner == localPlayer;
        }

        private void OnLocalUserInfoChanged(AuthUserInfo userInfo)
        {
            if (!isSpawned || !isOwner)
                return;
            // Token refreshes report the same account
            var current = _authUserInfo.value;
            if (current != null && current.Id == userInfo.Id && current.Email == userInfo.Email && current.Name == userInfo.Name)
                return;
            _authUserInfo.value = NetworkUserInfo.FromAuthUserInfo(userInfo);
        }

        // Server hands a guest its pass for streaming
        public void SendGuestPass(string pass)
        {
            if (!isServer || !owner.HasValue)
                return;
            TargetSetGuestPass(owner.Value, pass);
        }

        [TargetRpc]
        private void TargetSetGuestPass(PlayerID target, string pass)
        {
            if (TryGetPresence(out var presence))
                presence.SetGuestPass(pass);
        }

        private void BindPresence()
        {
            if (!TryGetPresence(out _) || !AppCore.Services.TryGet(out _serverManager))
                return;
            _serverManager.CurrentServerChanged += OnPresenceServerChanged;
            OnPresenceServerChanged(_serverManager.CurrentServer);
        }

        private void UnbindPresence()
        {
            if (_serverManager != null)
                _serverManager.CurrentServerChanged -= OnPresenceServerChanged;
            _serverManager = null;
            if (_presenceServer != null)
                _presenceServer.StateChanged -= OnPresenceServerStateChanged;
            _presenceServer = null;

            if (!TryGetPresence(out var presence))
                return;
            presence.SetJoinCode(null);
            presence.SetGuestPass(null);
        }

        private void OnPresenceServerChanged(ServerRecord server)
        {
            if (_presenceServer != null)
                _presenceServer.StateChanged -= OnPresenceServerStateChanged;
            _presenceServer = server;
            if (_presenceServer != null)
                _presenceServer.StateChanged += OnPresenceServerStateChanged;
            OnPresenceServerStateChanged(server);
        }

        // Join code can arrive after spawn on a host
        private void OnPresenceServerStateChanged(ServerRecord server)
        {
            if (TryGetPresence(out var presence))
                presence.SetJoinCode(_presenceServer?.JoinCode);
        }

        private static bool TryGetPresence(out PresenceService presence)
        {
            presence = AppCore.Services.TryGet<ServiceHubManager>(out var serviceHubManager) ? serviceHubManager.Presence : null;
            return presence != null;
        }

        private void OnAuthUserInfoChanged(NetworkUserInfo userInfo)
        {
            RefreshNameTag(userInfo);
        }

        private void RefreshNameTag(NetworkUserInfo userInfo)
        {
            if (_NameTag)
                _NameTag.SetText(userInfo?.Name);
        }
    }
}
