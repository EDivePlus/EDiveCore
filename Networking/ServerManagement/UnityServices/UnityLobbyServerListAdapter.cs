// Author: František Holubec
// Created: 08.08.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityLobbyServerListAdapter : AServerListAdapter
    {
        private CancellationTokenSource _searchCancellation;
        private CancellationTokenSource _heartbeatCancellation;

        private Lobby _hostLobby;
        private float _lastQueryTime;
        private AServerEndpoint[] _localEndpoints;
        
        public override async UniTask Initialize()
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        
        public override async UniTask PrepareServerStart()
        {
            await base.PrepareServerStart();
            await RegisterRelay();
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
            if (UnityEngine.Time.realtimeSinceStartup - _lastQueryTime < 2f)
                await UniTask.Delay(TimeSpan.FromSeconds(2), true, cancellationToken: cancellationToken);
                
            while (!cancellationToken.IsCancellationRequested)
            {
                var options = new QueryLobbiesOptions
                {
                    Count = 5,
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };

                _lastQueryTime = UnityEngine.Time.realtimeSinceStartup;
                var response = await LobbyService.Instance.QueryLobbiesAsync(options);
                SetServers(BuildRecords(response.Results));
                await UniTask.Delay(TimeSpan.FromSeconds(4), true, cancellationToken: cancellationToken);
            }
        }

        private static IEnumerable<ServerRecord> BuildRecords(IEnumerable<Lobby> lobbies)
        {
            foreach (var lobby in lobbies)
            {
                if (!lobby.Data.TryGetValue("uniqueID", out var uniqueID) || !long.TryParse(uniqueID.Value, out var serverID))
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
                    ServerID = serverID,
                    ServerName = lobby.Name,
                    CurrentPlayers = lobby.Players.Count,
                    MaxPlayers = lobby.MaxPlayers,
                    Endpoints = endpoints,
                };
            }
        }
        
        private async UniTask RegisterRelay()
        {
            var networkManager = InstanceFinder.NetworkManager;

            await Unity.Services.Core.UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            var allocation = await RelayService.Instance.CreateAllocationAsync(_serverConfig.MaxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var unityTransport = networkManager.TransportManager.GetTransport<UnityTransport>();
            var serverData = allocation.ToRelayServerData("dtls");
            unityTransport.SetRelayServerData(serverData);

            var publicIP = await GetPublicIPAsync();
            var tugboat = networkManager.TransportManager.GetTransport<Tugboat>();
            var publicPort = tugboat != null ? tugboat.GetPort() : (ushort)0;
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "uniqueID", new DataObject(DataObject.VisibilityOptions.Public, _serverConfig.ServerID.ToString()) },
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    { "publicIP", new DataObject(DataObject.VisibilityOptions.Public, publicIP) },
                    { "publicPort", new DataObject(DataObject.VisibilityOptions.Public, publicPort.ToString()) }
                }
            };

            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(_serverConfig.ServerName, _serverConfig.MaxPlayers + 1, options);

            var endpoints = new List<AServerEndpoint>();
            if (!string.IsNullOrEmpty(publicIP) && publicPort > 0)
            {
                endpoints.Add(new AddressServerEndpoint
                {
                    Name = "Unity Direct",
                    Address = publicIP,
                    Port = publicPort,
                });
            }
            endpoints.Add(new UnityRelayServerEndpoint
            {
                Name = "Unity Relay",
                Lobby = _hostLobby,
                RelayJoinCode = joinCode,
            });
            _localEndpoints = endpoints.ToArray();

            _heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            UniTask.Void(async cancellationToken =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(25), true, cancellationToken: cancellationToken);
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
            }, _heartbeatCancellation.Token);
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
        
        private static async UniTask<string> GetPublicIPAsync()
        {
            using var www = UnityWebRequest.Get("https://api.ipify.org");
            var request = await www.SendWebRequest();
            return request.result == UnityWebRequest.Result.Success ? www.downloadHandler.text.Trim() : string.Empty;
        }
    }
}
