// Author: František Holubec
// Created: 21.11.2025

using System;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.UTP;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityRelayServerEndpoint : AServerEndpoint
    {
        public Lobby Lobby;
        public string RelayJoinCode;
        public override string EndpointText => RelayJoinCode;
        
        public override async UniTask<bool> PrepareForConnect()
        {
            if (string.IsNullOrEmpty(RelayJoinCode))
                return false;

            var transportManager = InstanceFinder.TransportManager;
            var unityTransport = transportManager.GetTransport<UnityTransport>();
            if (unityTransport == null)
                return false;

            try
            {
                var allocation = await RelayService.Instance.JoinAllocationAsync(RelayJoinCode);
                unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

                var multiPass = transportManager.GetTransport<Multipass>();
                multiPass?.SetClientTransport<UnityTransport>();

                Debug.Log($"[ServerEndpoint] Connect using relay code {RelayJoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}
