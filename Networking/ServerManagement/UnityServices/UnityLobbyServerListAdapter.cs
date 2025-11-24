// Author: František Holubec
// Created: 08.08.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
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

        public override async UniTask Initialize()
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public override void StartServer()
        {
            base.StartServer();
            RegisterRelay().Forget();
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

        private async UniTaskVoid SearchTask(CancellationToken cancellationToken)
        {
            if (Time.realtimeSinceStartup - _lastQueryTime < 2f)
                await UniTask.Delay(TimeSpan.FromSeconds(2), true, cancellationToken: cancellationToken);
                
            while (!cancellationToken.IsCancellationRequested)
            {
                Servers.Clear();
                var options = new QueryLobbiesOptions
                {
                    Count = 5,
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };
                
                _lastQueryTime = Time.realtimeSinceStartup;
                var response = await LobbyService.Instance.QueryLobbiesAsync(options);
                foreach (var lobby in response.Results)
                {
                    if (!lobby.Data.TryGetValue("uniqueID", out var uniqueID) || !long.TryParse(uniqueID.Value, out var serverID))
                        continue;
                
                    AddServer(new UnityLobbyServerRecord
                    {
                        ServerID = serverID,
                        ServerName = lobby.Name,
                        CurrentPlayers = lobby.Players.Count,
                        MaxPlayers = lobby.MaxPlayers,
                        Lobby = lobby,
                        RelayJoinCode = lobby.Data.TryGetValue("joinCode", out var joinCode) ? joinCode.Value : string.Empty,
                        DirectConnectAddress = lobby.Data.TryGetValue("publicIP", out var publicIP) ? publicIP.Value : string.Empty,
                    });
                }
                await UniTask.Delay(TimeSpan.FromSeconds(4), true, cancellationToken: cancellationToken);
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
            unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

            var publicIP = await GetPublicIPAsync();
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "uniqueID", new DataObject(DataObject.VisibilityOptions.Public, _serverConfig.ServerID.ToString()) },
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                    { "publicIP", new DataObject(DataObject.VisibilityOptions.Public, publicIP) }
                }
            };

            _hostLobby = await LobbyService.Instance.CreateLobbyAsync(_serverConfig.ServerName, _serverConfig.MaxPlayers + 1, options);
            
            _heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            UniTask.Void(async cancellationToken =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(25), true, cancellationToken: cancellationToken);
                    if (_hostLobby == null) 
                        break;
                    await LobbyService.Instance.SendHeartbeatPingAsync(_hostLobby.Id);
                }
            }, _heartbeatCancellation.Token);
        }
        
        private async UniTask StopRelay()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = null;
            await LobbyService.Instance.DeleteLobbyAsync(_hostLobby.Id);
        }
        
        private static async UniTask<string> GetPublicIPAsync()
        {
            using var www = UnityWebRequest.Get("https://api.ipify.org");
            var request = await www.SendWebRequest();
            return request.result == UnityWebRequest.Result.Success ? www.downloadHandler.text.Trim() : string.Empty;
        }
    }
}
