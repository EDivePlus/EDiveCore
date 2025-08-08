// Author: František Holubec
// Created: 08.08.2025

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityRelay
{
    public class UnityRelayServerListAdapter : AServerListAdapter
    {
        public override UniTask Initialize(ServerConfig serverConfig)
        {
            throw new System.NotImplementedException();
        }

        public override void StartSearch()
        {
            throw new System.NotImplementedException();
        }

        public override void StopSearch()
        {
            throw new System.NotImplementedException();
        }
        
        public async UniTask<string> StartHostWithRelay(int maxConnections, string connectionType)
        {
            var networkManager = InstanceFinder.NetworkManager;
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Request allocation and join code
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            // Configure transport
            var unityTransport = networkManager.TransportManager.GetTransport<UnityTransport>();
            unityTransport.SetRelayServerData(allocation.ToRelayServerData(connectionType));

            
            var lobbyName = "new lobby";
            var maxPlayers = 4;
            var options = new CreateLobbyOptions
            {
                IsPrivate = false
            };

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Lobby created: {lobby.Id}");
        
            // Start host
            if (networkManager.ServerManager.StartConnection()) // Server is successfully started.
            {
                networkManager.ClientManager.StartConnection(); // You can choose not to call this method. Then only the server will start.
                return joinCode;
            }
            return null;
        }
        
        public async UniTask<bool> StartClientWithRelay(string joinCode, string connectionType)
        {
            var networkManager = InstanceFinder.NetworkManager;
            
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
            networkManager.GetComponent<UnityTransport>().SetRelayServerData(allocation.ToRelayServerData(connectionType));
            return !string.IsNullOrEmpty(joinCode) && networkManager.ClientManager.StartConnection();;
        }
        
        public async UniTask ListLobbies()
        {
            var options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>()
                {
                    new(field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0")
                },
                Order = new List<QueryOrder>()
                {
                    new(asc: false,
                        field: QueryOrder.FieldOptions.Created)
                }
            };

            var response = await LobbyService.Instance.QueryLobbiesAsync(options);
            foreach (var lobby in response.Results)
            {
                Debug.Log($"Lobby: {lobby.Name}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}");
            }
        }
        
        public async UniTask JoinLobby(string lobbyId)
        {
            var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log($"Joined Lobby: {lobby.Name}");
        }
    }
}
