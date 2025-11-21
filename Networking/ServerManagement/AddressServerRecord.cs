// Author: František Holubec
// Created: 14.07.2025

using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;

namespace EDIVE.Networking.ServerManagement
{
    public class AddressServerRecord : AServerRecord
    {
        public string Address;
        
        public override UniTask<bool> PrepareForConnect()
        {
            var transportManager = InstanceFinder.TransportManager;
            var multiPass = transportManager.GetTransport<Multipass>();
            if (multiPass != null) 
                multiPass.SetClientTransport<Tugboat>();
            
            var tugboat = transportManager.GetTransport<Tugboat>();
            if (tugboat == null) 
                return UniTask.FromResult(false);
            
            tugboat.SetClientAddress(Address);
            return UniTask.FromResult(true);
        }
    }
}
