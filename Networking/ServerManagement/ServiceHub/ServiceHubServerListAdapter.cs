// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Core.Versions;
using EDIVE.Networking.ServerManagement.PurrNet;
using EDIVE.Networking.ServerManagement.UnityServices;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Lobby;
using PurrNet;
using PurrNet.Purrnity;
using PurrNet.Transports;
using PurrNet.UTP;
using Sirenix.OdinInspector;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
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
        [MinValue(1f)]
        [Tooltip("How many lobbies to fetch per query.")]
        private int _QueryCount = 5;
        
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("Interval between heartbeat updates. Backend default TTL is ~40s.")]
        private float _HeartbeatInterval = 15f;
        
        [SerializeField]
        private UnityRelayAllocator _RelayAllocator;
        
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        private LobbyService _lobby;
        
        private CancellationTokenSource _searchCancellation;
        private CancellationTokenSource _heartbeatCancellation;
        private CancellationTokenSource _probeCancellation;
        
        private float _lastQueryTime;
        private AServerEndpoint[] _localEndpoints;
        
        private string _serverSecret;
        private bool _disposing;
        private DateTime _lastSuccessfulContactUtc;
        
        private string _unityRelayJoinCode = string.Empty;
        private string _purrRelayRoom = string.Empty;
        private bool _relayInitialized;

        public override UniTask Initialize()
        {
            base.Initialize();
            _lobby = AppCore.Services.Get<ServiceHubManager>().Lobby;
            return UniTask.CompletedTask;
        }

        public override async UniTask PrepareServerStart()
        {
            await base.PrepareServerStart();
            await RegisterLobby(destroyCancellationToken);
            
            _heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            HeartbeatTask(_heartbeatCancellation.Token).Forget();
        }
        
        private void OnDestroy()
        {
            _disposing = true;
            DisposeAsync().AsTask();
        }

        public override void StopServer()
        {
            base.StopServer();
            if (!_disposing)
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
            // Ensure we don't spam the lobby service with queries if search is stopped and started again quickly.
            if (UnityEngine.Time.realtimeSinceStartup - _lastQueryTime < 2f)
                await UniTask.Delay(TimeSpan.FromSeconds(2), true, cancellationToken: cancellationToken);
                
            while (!cancellationToken.IsCancellationRequested)
            {
                _lastQueryTime = UnityEngine.Time.realtimeSinceStartup;
                var response = await _lobby.QueryServersAsync(
                    new QueryServersRequest { Count = _QueryCount, Skip = 0 },
                    cancellationToken);
                var records = response.IsSuccess && response.Result != null
                    ? BuildRecords(response.Result)
                    : Array.Empty<ServerRecord>();
                SetServers(records);
                await UniTask.Delay(TimeSpan.FromSeconds(_QueryInterval), true, cancellationToken: cancellationToken);
            }
        }

        private static IEnumerable<ServerRecord> BuildRecords(IEnumerable<LobbyServerResponse> lobbies)
        {
            foreach (var lobby in lobbies)
            {
                var endpoints = new List<AServerEndpoint>();
                if (!string.IsNullOrWhiteSpace(lobby.PublicAddress) && lobby.PublicPort > 0)
                {
                    endpoints.Add(new AddressServerEndpoint
                    {
                        Name = "Remote Direct",
                        Address = lobby.PublicAddress,
                        Port = (ushort) lobby.PublicPort
                    });
                }
                
                var data = ServiceHubServerData.TryParse(lobby.Data);
                if (data == null || string.IsNullOrEmpty(data.InstanceID))
                    continue;
                
                if (!string.IsNullOrEmpty(data.UnityRelayCode))
                {
                    endpoints.Add(new UnityRelayServerEndpoint
                    {
                        Name = "Unity Relay",
                        RelayJoinCode = data.UnityRelayCode,
                    });
                }
                
                if (!string.IsNullOrEmpty(data.PurrRelayRoom))
                {
                    endpoints.Add(new PurrRelayServerEndpoint
                    {
                        Name = "Purr Relay",
                        RoomName = data.PurrRelayRoom
                    });
                }
                
                yield return new ServerRecord
                {
                    InstanceID = data.InstanceID,
                    ServerName = lobby.Name,
                    CurrentPlayers = lobby.CurrentPlayers,
                    MaxPlayers = lobby.MaxPlayers,
                    Endpoints = endpoints
                };
            }
        }

        private async UniTask RegisterLobby(CancellationToken cancellationToken = default)
        {
            var transports = await AppCore.Services.AwaitRegistered<TransportController>();

            if (!_relayInitialized)
            {
                if (_RelayAllocator != null && (transports.HasTransport<PurrnityTransport>() || transports.HasTransport<UTPTransport>()))
                {
                    try
                    {
                        var allocation = await _RelayAllocator.GetAllocationAsync();
                        _unityRelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                        if (transports.TryGetTransport<PurrnityTransport>(out var unityTransport))
                        {
                            var serverData = allocation.ToRelayServerData("dtls");
                            unityTransport.SetRelayServerData(serverData);
                        }

                        if (transports.TryGetTransport<UTPTransport>(out var utpTransport))
                            utpTransport.InitializeRelayServer(allocation);

                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                if (transports.TryGetTransport<PurrTransport>(out var purrTransport))
                {
                    _purrRelayRoom = Guid.NewGuid().ToString().Replace("-", "");
                    purrTransport.roomName = _purrRelayRoom;
                }
                
                _relayInitialized = true;
            }

            var data = new ServiceHubServerData
            {
                InstanceID = _serverConfig.InstanceID,
                UnityRelayCode = _unityRelayJoinCode,
                PurrRelayRoom = _purrRelayRoom
            };
            var response = await _lobby.RegisterServerAsync(new RegisterServerRequest
            {
                Name = _serverConfig.ServerName,
                Version = _VersionDefinition != null ? _VersionDefinition.VersionString : "0",
                PublicAddress = _serverConfig.PublicAddress,
                PublicPort = _serverConfig.ResolvedPort,
                Data = data.Serialize(),
                IsPrivate = _serverConfig.IsPrivate,
                IsDebug = Debug.isDebugBuild
            }, cancellationToken);
            
            if (!response.IsSuccess || response.Result == null)
            {
                Debug.LogError($"[ServiceHubServerListAdapter] Server registration failed: {response.ErrorMessage}");
                return;
            }
            _serverSecret = response.Result.Secret;
            _lastSuccessfulContactUtc = DateTime.UtcNow;

            var endpoints = new List<AServerEndpoint>();
            if (!string.IsNullOrEmpty(_serverConfig.PublicAddress) && _serverConfig.ResolvedPort > 0)
            {
                endpoints.Add(new AddressServerEndpoint
                {
                    Name = "Remote Direct",
                    Address = _serverConfig.PublicAddress,
                    Port = _serverConfig.ResolvedPort,
                });
            }
            
            if (!string.IsNullOrEmpty(_purrRelayRoom))
            {
                endpoints.Add(new PurrRelayServerEndpoint
                {
                    Name = "Purr Relay",
                    RoomName = _purrRelayRoom
                });
            }

            if (!string.IsNullOrEmpty(_unityRelayJoinCode))
            {
                endpoints.Add(new UnityRelayServerEndpoint
                {
                    Name = "Unity Relay",
                    RelayJoinCode = _unityRelayJoinCode,
                });
            }
            _localEndpoints = endpoints.ToArray();
        }
        
        private async UniTaskVoid HeartbeatTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, _HeartbeatInterval)), true, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(_serverSecret))
                    return;

                try
                {
                    var currentPlayers = NetworkManager.main.playerCount;

                    var request = new UpdateServerRequest{
                        Secret = _serverSecret,
                        Name = _serverConfig.ServerName,
                        CurrentPlayers = currentPlayers
                    };

                    var response = await _lobby.UpdateServerAsync(request, cancellationToken);
                    if (response.IsSuccess)
                    {
                        _lastSuccessfulContactUtc = DateTime.UtcNow;
                    }
                    else if (response.ApiStatus is (int) ServiceHubStatus.InvalidSecret or (int) ServiceHubStatus.ServerNotFound)
                    {
                        Debug.Log($"[ServiceHubServerListAdapter] Lobby entry no longer valid (api status {response.ApiStatus}: {response.ErrorMessage}) — re-registering server.");
                        await RegisterLobby(cancellationToken);
                    }
                    else
                    {
                        Debug.LogError($"[ServiceHubServerListAdapter] Heartbeat/update failed (status {response.StatusCode}): {response.ErrorMessage}");
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    if ((DateTime.UtcNow - _lastSuccessfulContactUtc).TotalSeconds >= Mathf.Max(1f, _HeartbeatInterval))
                    {
                        Debug.Log($"[ServiceHubServerListAdapter] Lobby entry lost (exception: {e.Message}) — re-registering server.");
                        await RegisterLobby(cancellationToken);
                    }
                    else
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private async UniTask DisposeAsync()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = null;
            _localEndpoints = null;
            
            if (string.IsNullOrEmpty(_serverSecret) || _lobby == null)
                return;

            var secret = _serverSecret;
            _serverSecret = null;

            try
            {
                var response = await _lobby.DisposeServerAsync(secret, CancellationToken.None);
                if (!response.IsSuccess)
                    Debug.LogWarning($"[ServiceHubServerListAdapter] Dispose failed: {response.ErrorMessage}");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogWarning($"[ServiceHubServerListAdapter] Dispose exception: {e.Message}");
            }
        }
    }
}
