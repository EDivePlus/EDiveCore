// Author: František Holubec
// Created: 13.05.2026

using EDIVE.Core.Services;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class TransportController : AServiceBehaviour<TransportController>
    {
        [SerializeField]
        private CompositeTransport _CompositeTransport;
        
        [SerializeField]
        private LocalTransport _LocalTransport;

        public bool TryGetTransport<T>(out T transport) where T : GenericTransport
        {
            if (_LocalTransport is T local)
            {
                transport = local;
                return true;
            }
            
            return _CompositeTransport.TryGetTransport(out transport);
        }
        
        public bool TrySetTransport<T>(out T transport) where T : GenericTransport
        {
            if (!TryGetTransport(out transport) || NetworkManager.main == null) 
                return false;
            
            NetworkManager.main.transport = transport;
            return true;
        }
        
        public CompositeTransport SetCompositeTransport()
        {
            if (NetworkManager.main != null) 
                NetworkManager.main.transport = _CompositeTransport;
            return _CompositeTransport;
        }
        
        public LocalTransport SetLocalTransport()
        {
            if (NetworkManager.main != null) 
                NetworkManager.main.transport = _LocalTransport;
            return _LocalTransport;
        }
    }
}
