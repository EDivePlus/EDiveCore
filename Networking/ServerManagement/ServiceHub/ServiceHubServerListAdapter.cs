// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Core.Versions;
using EDIVE.Networking.ServerManagement.UnityServices;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Lobby;
using FishNet;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.ServiceHub
{
    public class ServiceHubServerListAdapter : AServerListAdapter
    {
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("How often the lobby is queried while searching is active.")]
        private float _QueryInterval = 4f;

        [SerializeField]
        [MinValue(1)]
        [Tooltip("Maximum number of servers to fetch per query.")]
        private int _QueryCount = 20;
        
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("Interval between heartbeat updates. Backend default TTL is ~40s.")]
        private float _HeartbeatInterval = 15f;
        
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        [ReadOnly]
        [ShowInInspector]
        public string RegisteredJoinCode { get; private set; }

        [ReadOnly]
        [ShowInInspector]
        public bool IsRegistered => !string.IsNullOrEmpty(_serverSecret);

        private CancellationTokenSource _searchCancellation;
        private CancellationTokenSource _heartbeatCancellation;
        private string _serverSecret;

        private LobbyService _lobby;
        private AServerEndpoint[] _localEndpoints;

        public override async UniTask Initialize()
        {
            var hub = await AppCore.Services.AwaitRegistered<ServiceHubManager>();
            _lobby = hub.Lobby;
        }

        public override async UniTask PrepareServerStart()
        {
            await base.PrepareServerStart();
            await RegisterAsync(destroyCancellationToken);
        }

        public override void StopServer()
        {
            base.StopServer();
            DisposeAsync().Forget();
        }

        public override void StartSearch()
        {
            if (_searchCancellation != null)
                return;

            _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            SearchTask(_searchCancellation.Token).Forget();
        }

        public override void StopSearch()
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = null;
        }
        
        public override IEnumerable<AServerEndpoint> GetLocalServerEndpoints()
            => _localEndpoints ?? Array.Empty<AServerEndpoint>();

        private async UniTaskVoid SearchTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_lobby != null)
                    {
                        var response = await _lobby.QueryServersAsync(_QueryCount, 0, cancellationToken);
                        if (response.IsSuccess && response.Result != null)
                            SetServers(BuildRecords(response.Result));
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Debug.LogWarning($"[ServiceHubServerListAdapter] Query failed: {e.Message}");
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_QueryInterval), true, cancellationToken: cancellationToken);
            }
        }

        private static IEnumerable<ServerRecord> BuildRecords(IEnumerable<LobbyServerResponse> responses)
        {
            foreach (var item in responses)
            {
                if (item == null)
                    continue;

                var parsed = ServiceHubServerData.TryParse(item.Data);
                var instanceID = !string.IsNullOrEmpty(parsed?.InstanceID) ? parsed.InstanceID : item.JoinCode;
                if (string.IsNullOrEmpty(instanceID))
                    continue;

                var endpoints = new List<AServerEndpoint>();
                if (!string.IsNullOrEmpty(item.PublicAddress) && item.PublicPort is > 0)
                {
                    endpoints.Add(new AddressServerEndpoint
                    {
                        Name = "Remote Direct",
                        Address = item.PublicAddress,
                        Port = (ushort) item.PublicPort.Value,
                    });
                }

                yield return new ServerRecord
                {
                    InstanceID = instanceID,
                    ServerName = item.Name,
                    CurrentPlayers = parsed?.CurrentPlayers ?? 0,
                    MaxPlayers = parsed?.MaxPlayers ?? 0,
                    Endpoints = endpoints,
                };
            }
        }

        private async UniTask RegisterAsync(CancellationToken cancellationToken)
        {
            if (_serverConfig == null || _lobby == null)
                return;

            var hostServer = AppCore.Services.TryGet<NetworkServerManager>(out var nsm) ? nsm.HostServer : null;
            var (publicAddress, publicPort, relayCode) = ResolveEndpoints(hostServer);

            var currentPlayers = hostServer?.CurrentPlayers ?? 0;
            var data = new ServiceHubServerData(_serverConfig.InstanceID, currentPlayers, _serverConfig.MaxPlayers).Serialize();

            var request = new RegisterServerRequest(
                name: _serverConfig.ServerName,
                version: _VersionDefinition != null ? _VersionDefinition.VersionString : "0",
                publicAddress: publicAddress,
                publicPort: publicPort,
                relayCode: relayCode,
                data: data,
                isPrivate: false,
                joinCode: null,
                isDebug: Debug.isDebugBuild
            );

            var response = await _lobby.RegisterServerAsync(request, cancellationToken);
            if (!response.IsSuccess || response.Result == null)
            {
                Debug.LogError($"[ServiceHubServerListAdapter] Server registration failed: {response.ErrorMessage}");
                return;
            }

            _serverSecret = response.Result.Secret;
            RegisteredJoinCode = response.Result.Code;
            Debug.Log($"[ServiceHubServerListAdapter] Registered with join code {RegisteredJoinCode}");

            // Add local endpoints for clients to connect with
            var endpoints = new List<AServerEndpoint>();
            if (!string.IsNullOrEmpty(publicAddress) && publicPort is > 0)
            {
                endpoints.Add(new AddressServerEndpoint
                {
                    Name = "Remote Direct",
                    Address = publicAddress,
                    Port = (ushort) publicPort.Value,
                });
            }
            _localEndpoints = endpoints.ToArray();
            
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            HeartbeatLoop(_heartbeatCancellation.Token).Forget();
        }

        private async UniTaskVoid HeartbeatLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, _HeartbeatInterval)), true, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(_serverSecret))
                    return;

                try
                {
                    var currentPlayers = InstanceFinder.ServerManager.Clients.Count;
                    var data = new ServiceHubServerData(_serverConfig.InstanceID, currentPlayers, _serverConfig.MaxPlayers).Serialize();

                    var request = new UpdateServerRequest(
                        secret: _serverSecret,
                        name: _serverConfig.ServerName,
                        data: data,
                        currentPlayers: currentPlayers,
                        maxPlayers: _serverConfig.MaxPlayers
                    );

                    var response = await _lobby.UpdateServerAsync(request, cancellationToken);
                    if (!response.IsSuccess)
                        Debug.LogWarning($"[ServiceHubServerListAdapter] Heartbeat/update failed: {response.ErrorMessage}");
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Debug.LogWarning($"[ServiceHubServerListAdapter] Heartbeat exception: {e.Message}");
                }
            }
        }

        private async UniTask DisposeAsync()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = null;

            if (string.IsNullOrEmpty(_serverSecret) || _lobby == null)
                return;

            var secret = _serverSecret;
            _serverSecret = null;
            RegisteredJoinCode = null;

            try
            {
                var response = await _lobby.DisposeServerAsync(secret);
                if (!response.IsSuccess)
                    Debug.LogWarning($"[ServiceHubServerListAdapter] Dispose failed: {response.ErrorMessage}");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogWarning($"[ServiceHubServerListAdapter] Dispose exception: {e.Message}");
            }
        }

        private static (string address, int? port, string relayCode) ResolveEndpoints(ServerRecord hostServer)
        {
            if (hostServer?.Endpoints == null)
                return (null, null, null);

            string address = null;
            int? port = null;
            string relayCode = null;

            foreach (var endpoint in hostServer.Endpoints)
            {
                switch (endpoint)
                {
                    case AddressServerEndpoint addr when !string.IsNullOrEmpty(addr.Address) && addr.Port > 0:
                        address ??= addr.Address;
                        port ??= addr.Port;
                        break;
                    case UnityRelayServerEndpoint relay when !string.IsNullOrEmpty(relay.RelayJoinCode):
                        relayCode ??= relay.RelayJoinCode;
                        break;
                }
            }

            return (address, port, relayCode);
        }
    }
}
