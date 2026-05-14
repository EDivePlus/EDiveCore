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
using PurrNet;
using Sirenix.OdinInspector;
using Unity.Services.Relay;
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
                
                if (!string.IsNullOrEmpty(data.RelayCode))
                {
                    endpoints.Add(new UnityRelayServerEndpoint
                    {
                        Name = "Unity Relay",
                        RelayJoinCode = data.RelayCode,
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
            var joinCode = string.Empty;
            try
            {
                var allocation = await _RelayAllocator.GetAllocationAsync();
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            var data = new ServiceHubServerData
            {
                InstanceID = _serverConfig.InstanceID,
                RelayCode = joinCode
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
            _localEndpoints = null;
            
            if (string.IsNullOrEmpty(_serverSecret) || _lobby == null)
                return;

            var secret = _serverSecret;
            _serverSecret = null;

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
    }
}
