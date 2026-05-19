// Author: František Holubec
// Created: 21.11.2025

#if UNITY_SERVICES && UNITY_TRANSPORT
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using PurrNet.UTP;
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
            
            if (transportController.TryGetTransport<UTPTransport>(out var utpTransport))
            {
                await utpTransport.InitializeRelayClient(RelayJoinCode);
                transportController.SetClient(utpTransport);
                
                Debug.Log($"[ServerEndpoint] Connect using Unity relay (UTP) '{RelayJoinCode}'");
                return true;
            }
            
            return false;
        }
    }
}
#endif
