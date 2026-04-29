// Author: František Holubec
// Created: 14.07.2025

using Cysharp.Threading.Tasks;
using EDIVE.Networking.Utils;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
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

            var transportManager = InstanceFinder.TransportManager;
            var tugboat = transportManager.GetTransport<Tugboat>();
            if (tugboat == null)
                return false;

            var multiPass = transportManager.GetTransport<Multipass>();
            multiPass?.SetClientTransport<Tugboat>();

            tugboat.SetClientAddress(Address);
            if (Port > 0)
                tugboat.SetPort(Port);

            Debug.Log($"[ServerEndpoint] Connect using direct address {Address}:{tugboat.GetPort()}");
            return true;
        }
    }
}
