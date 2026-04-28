// Author: František Holubec
// Created: 14.07.2025

using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class AddressServerRecord : ADirectServerRecord
    {
        public override UniTask<bool> PrepareForConnect()
        {
            var transportManager = InstanceFinder.TransportManager;
            var multiPass = transportManager.GetTransport<Multipass>();
            if (multiPass != null)
                multiPass.SetClientTransport<Tugboat>();

            var tugboat = transportManager.GetTransport<Tugboat>();
            if (tugboat == null)
                return UniTask.FromResult(false);

            tugboat.SetClientAddress(DirectAddress);
            if (DirectPort > 0)
                tugboat.SetPort(DirectPort);

            Debug.Log($"[ServerRecord] Connect using direct address {DirectAddress}:{tugboat.GetPort()}");
            return UniTask.FromResult(true);
        }
    }
}
