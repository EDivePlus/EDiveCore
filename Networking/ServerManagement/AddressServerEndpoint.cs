// Author: František Holubec
// Created: 14.07.2025

using Cysharp.Threading.Tasks;
using EDIVE.Networking.Utils;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class AddressServerEndpoint : AServerEndpoint
    {
        public string Address;
        public ushort Port;
        
        public override string EndpointText => $"{Address}:{Port}";

        public override async UniTask<bool> PrepareForConnect()
        {
            if (string.IsNullOrEmpty(Address))
                return false;

            if (Port > 0 && !await NetworkUtils.IsServerReachable(Address, Port))
                return false;
            
            if (!NetworkUtils.TrySetClientTransport<UDPTransport>(out var udp))
                return false;

            udp.address = Address;
            if (Port > 0)
                udp.serverPort = Port;

            Debug.Log($"[ServerEndpoint] Connect using direct address {Address}:{udp.serverPort}");
            return true;
        }
    }
}
