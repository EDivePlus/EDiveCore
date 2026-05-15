// Author: František Holubec
// Created: 13.05.2026

using System.Collections.Generic;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class TransportController : AServiceBehaviour<TransportController>
    {
        [SerializeField]
        private List<GenericTransport> _MainTransports;

        public bool HasTransport<T>() where T : GenericTransport
        {
            return TryGetTransport(out T _);
        }
        
        public bool TryGetTransport<T>(out T transport) where T : GenericTransport
        {
            if (_MainTransports.TryGetFirstT(out transport))
                return true;
            
            if (TryGetTransport<CompositeTransport>(out var composite))
                return composite.TryGetTransport(out transport);
            
            return false;
        }
        
        public bool TrySetTransport(GenericTransport transport)
        {
            if (transport == null || NetworkManager.main == null) 
                return false;
            
            if (TryGetTransport<CompositeTransport>(out var composite))
            {
                NetworkManager.main.transport = composite;
                composite.SetClientTransport(transport);
                return true;
            }

            NetworkManager.main.transport = transport;
            return true;
        }
        
        public bool TrySetTransport<T>() where T : GenericTransport
        {
            return TrySetTransport<T>(out _);
        }
        
        public bool TrySetTransport<T>(out T transport) where T : GenericTransport
        {
            if (!TryGetTransport(out transport) || NetworkManager.main == null) 
                return false;
            
            return TrySetTransport(transport);
        }
    }
}
