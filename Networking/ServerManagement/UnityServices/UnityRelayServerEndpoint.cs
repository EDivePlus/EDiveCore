// Author: František Holubec
// Created: 21.11.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using PurrNet.Purrnity;
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
            
            if (!AppCore.Services.TryGet<TransportController>(out var transportController))
                return false;
            
            if (!transportController.TryGetTransport<PurrnityTransport>(out var unityTransport))
                return false;

            try
            {
                var allocation = await RelayService.Instance.JoinAllocationAsync(RelayJoinCode);
                unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

                var composite = transportController.SetCompositeTransport();
                composite.SetClientTransport(unityTransport);

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
