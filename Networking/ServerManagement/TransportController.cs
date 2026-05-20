// Author: František Holubec
// Created: 13.05.2026

using EDIVE.Core.Services;
using PurrNet;
using PurrNet.Transports;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public class TransportController : AServiceBehaviour<TransportController>
    {
        [Required]
        [SerializeField]
        private CompositeTransport _CompositeTransports;
        
        [Required]
        [SerializeField]
        private LocalTransport _LocalTransport;

        public bool HasTransport<T>() where T : GenericTransport
        {
            return TryGetTransport<T>(out _);
        }
        
        public bool TryGetTransport<T>(out T transport) where T : GenericTransport
        {
            transport = null;
            if (_LocalTransport is T local)
            {
                transport = local;
                return true;
            }
            
            return _CompositeTransports != null && _CompositeTransports.TryGetTransport(out transport);
        }
        
        public void SetServer()
        {
            NetworkManager.main.transport = _CompositeTransports;
        }
        
        public void SetHost()
        {
            NetworkManager.main.transport = _CompositeTransports;
            // Host mode defaults to UDPTransport, LocalTransport seems to not work.
            // _CompositeTransports.SetClientTransport<LocalTransport>();
        }
        
        public void SetOffline()
        {
            NetworkManager.main.transport = _LocalTransport;
        }
        
        public void SetClient(GenericTransport transport)
        {
            NetworkManager.main.transport = _CompositeTransports;
            if (transport is CompositeTransport)
            {                
                Debug.LogError("[TransportController] CompositeTransport is not valid for Client");
                return;
            }
           
            _CompositeTransports.SetClientTransport(transport);
        }

        public bool TrySetClient<T>() where T : GenericTransport
        {
            return TrySetClient<T>(out _);
        }

        public bool TrySetClient<T>(out T transport) where T : GenericTransport
        {
            if (!TryGetTransport(out transport)) 
                return false;
            
            SetClient(transport);
            return true;
        }

        public SessionLinkMode GetSessionLinkMode()
        {
            if (TryGetTransport<PurrTransport>(out var purrTransport) && purrTransport.clientState == ConnectionState.Connected)
            {
                return purrTransport.clientSessionLink switch
                {
                    PurrTransport.SessionLink.P2P => SessionLinkMode.Direct,
                    PurrTransport.SessionLink.Relay => SessionLinkMode.Relay,
                    _ => SessionLinkMode.None
                };
            }

            if (TryGetTransport<UDPTransport>(out var udpTransport) && udpTransport.clientState == ConnectionState.Connected)
                return SessionLinkMode.Direct;

            return SessionLinkMode.None;
        }
    }
}
