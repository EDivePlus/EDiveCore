// Author: František Holubec
// Created: 21.11.2025

using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.UTP;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace EDIVE.Networking.ServerManagement.UnityRelay
{
    public class UnityRelayServerRecord : AServerRecord
    {
        public Lobby Lobby;
        public string RelayJoinCode;
        
        public override async UniTask<bool> PrepareForConnect()
        {
            var transportManager = InstanceFinder.TransportManager;
            var multiPass = transportManager.GetTransport<Multipass>();
            if (multiPass != null) 
                multiPass.SetClientTransport<UnityTransport>();
            
            var unityTransport = transportManager.GetTransport<UnityTransport>();
            if (unityTransport == null)
                return false;
            
            var allocation = await RelayService.Instance.JoinAllocationAsync(RelayJoinCode);
            unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            return true;
        }
    }
}
