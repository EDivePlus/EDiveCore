// Author: František Holubec
// Created: 21.11.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using PurrNet.Purrnity;
using PurrNet.UTP;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityRelayServerEndpoint : AServerEndpoint
    {
        public string RelayJoinCode;
        public override string EndpointText => RelayJoinCode;
        
        public override async UniTask<bool> PrepareForConnect()
        {
            if (string.IsNullOrEmpty(RelayJoinCode))
                return false;
            
            if (!AppCore.Services.TryGet<TransportController>(out var transportController))
                return false;
            
            if (transportController.TryGetTransport<PurrnityTransport>(out var unityTransport))
            {
                try
                {
                    var allocation = await RelayService.Instance.JoinAllocationAsync(RelayJoinCode);
                    unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
                    transportController.TrySetTransport(unityTransport);
                    
                    Debug.Log($"[ServerEndpoint] Connect using Unity relay (Purrnity) '{RelayJoinCode}'");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
            if (transportController.TryGetTransport<UTPTransport>(out var utpTransport))
            {
                await utpTransport.InitializeRelayClient(RelayJoinCode);
                transportController.TrySetTransport(utpTransport);
                
                Debug.Log($"[ServerEndpoint] Connect using Unity relay (UTP) '{RelayJoinCode}'");
                return true;
            }
            
            return false;
        }
    }
}
