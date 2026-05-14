// Author: František Holubec
// Created: 08.08.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.Utils;
using EDIVE.UnityServices;
using PurrNet.Purrnity;
using PurrNet.Transports;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityLobbyServerListAdapter : AServerListAdapter
    {
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("How often the lobby is queried while searching is active.")]
        private float _QueryInterval = 4f;
        
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("How many lobbies to fetch per query. Note that Unity's lobby service only returns up to 100 lobbies total, so setting this too high may cause increased latency without improving results.")]
        private int _QueryCount = 5;
        
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("Interval between heartbeat updates. Backend default TTL is ~40s.")]
        private float _HeartbeatInterval = 25f;
        
        [SerializeField]
        private UnityRelayAllocator _RelayAllocator;
        
        private CancellationTokenSource _searchCancellation;
        private CancellationTokenSource _heartbeatCancellation;

        private Lobby _hostLobby;
        private float _lastQueryTime;
        private AServerEndpoint[] _localEndpoints;

        public override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(UnityServicesManager));
        }

        public override async UniTask PrepareServerStart()
        {
            await base.PrepareServerStart();
            await RegisterRelay();
            
            _heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            HeartbeatTask(_heartbeatCancellation.Token).Forget();
        }
        
        public override void StopServer()
        {
            base.StopServer();
            StopRelay().Forget();
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
                var options = new QueryLobbiesOptions
                {
                    Count = _QueryCount,
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };

                _lastQueryTime = UnityEngine.Time.realtimeSinceStartup;
                var response = await LobbyService.Instance.QueryLobbiesAsync(options);
                SetServers(BuildRecords(response.Results));
                await UniTask.Delay(TimeSpan.FromSeconds(_QueryInterval), true, cancellationToken: cancellationToken);
            }
        }

        private static IEnumerable<ServerRecord> BuildRecords(IEnumerable<Lobby> lobbies)
        {
            foreach (var lobby in lobbies)
            {
                if (!lobby.Data.TryGetValue("uniqueID", out var uniqueID) || string.IsNullOrEmpty(uniqueID.Value))
                    continue;

                ushort publicPort = 0;
                if (lobby.Data.TryGetValue("publicPort", out var publicPortData))
                    ushort.TryParse(publicPortData.Value, out publicPort);

                var publicAddress = lobby.Data.TryGetValue("publicIP", out var publicIP) ? publicIP.Value : string.Empty;
                var relayJoinCode = lobby.Data.TryGetValue("joinCode", out var joinCode) ? joinCode.Value : string.Empty;

                
                var endpoints = new List<AServerEndpoint>();
                if (!string.IsNullOrEmpty(publicAddress) && publicPort > 0)
                {
                    endpoints.Add(new AddressServerEndpoint
                    {
                        Name = "Unity Direct",
                        Address = publicAddress,
                        Port = publicPort,
                    });
                }
           
                endpoints.Add(new UnityRelayServerEndpoint
                {
                    Name = "Unity Relay",
                    Lobby = lobby,
                    RelayJoinCode = relayJoinCode,
                });

                yield return new ServerRecord
                {
                    InstanceID = uniqueID.Value,
                    ServerName = lobby.Name,
                    CurrentPlayers = lobby.Players.Count,
                    MaxPlayers = lobby.MaxPlayers,
                    Endpoints = endpoints,
                };
            }
        }
        
        private async UniTask RegisterRelay()
        {
            var transportController = await AppCore.Services.AwaitRegistered<TransportController>();
            var joinCode = string.Empty;
            try
            {
                var allocation = await _RelayAllocator.GetAllocationAsync();
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
                if (transportController.TryGetTransport<PurrnityTransport>(out var unityTransport))
                {
                    var serverData = allocation.ToRelayServerData("dtls");
                    unityTransport.SetRelayServerData(serverData);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            var lobbyData = new Dictionary<string, DataObject>
            {
                { "uniqueID", new DataObject(DataObject.VisibilityOptions.Public, _serverConfig.InstanceID) },
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
            };

            var options = new CreateLobbyOptions { IsPrivate = false, Data = lobbyData };
            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(_serverConfig.ServerName, _serverConfig.MaxPlayers + 1, options);
            var endpoints = new List<AServerEndpoint>();
            endpoints.Add(new UnityRelayServerEndpoint
            {
                Name = "Unity Relay",
                Lobby = _hostLobby,
                RelayJoinCode = joinCode,
            });
            _localEndpoints = endpoints.ToArray();
        }
        
        private async UniTaskVoid HeartbeatTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_HeartbeatInterval), true, cancellationToken: cancellationToken);
                if (_hostLobby == null)
                    break;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    Debug.LogWarning($"[UnityLobbyServerListAdapter] Lobby heartbeat failed: {e.Message}");
                }
            }
        }
        
        private async UniTask StopRelay()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = null;
            _localEndpoints = null;
            if (_hostLobby != null)
            {
                var lobbyId = _hostLobby.Id;
                _hostLobby = null;
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
        }
        
    }
}
