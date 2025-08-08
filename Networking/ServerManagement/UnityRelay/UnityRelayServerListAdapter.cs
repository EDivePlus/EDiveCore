// Author: František Holubec
// Created: 08.08.2025

using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

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
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            // Configure transport
            var unityTransport = networkManager.TransportManager.GetTransport<UnityTransport>();
            unityTransport.SetRelayServerData(allocation.ToRelayServerData(connectionType));

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
    }
}
