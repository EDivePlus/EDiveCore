// Author: František Holubec
// Created: 13.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        // Host client, must be one of the composite transports
        [Required]
        [SerializeField]
        private LocalTransport _HostLocalTransport;

        [SerializeField]
        [MinValue(1)]
        [Tooltip("Seconds between attempts to listen on the relay again after the host lost it.")]
        private float _RelayRetryDelay = 5f;

        private PurrTransport _relayTransport;
        private CancellationTokenSource _relayRetryCts;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_CompositeTransports.TryGetTransport(out _relayTransport))
                _relayTransport.transport.onConnectionState += OnRelayConnectionState;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_relayTransport)
                _relayTransport.transport.onConnectionState -= OnRelayConnectionState;

            _relayRetryCts?.Cancel();
            _relayRetryCts?.Dispose();
            _relayRetryCts = null;
        }

        // PurrTransport stops listening when its relay link drops, the rest of the server keeps running
        private void OnRelayConnectionState(ConnectionState state, bool asServer)
        {
            if (!asServer || state != ConnectionState.Disconnected || _relayRetryCts != null || !IsServerListening())
                return;

            _relayRetryCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RelistenRelayAsync(_relayRetryCts).Forget();
        }

        private async UniTaskVoid RelistenRelayAsync(CancellationTokenSource cts)
        {
            var relay = _relayTransport.transport;
            try
            {
                while (relay.listenerState == ConnectionState.Disconnected)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_RelayRetryDelay), true, cancellationToken: cts.Token);
                    if (!IsServerListening())
                        return;

                    Debug.LogWarning("[TransportController] Relay listener lost, listening again.");
                    relay.Listen(0);
                    await UniTask.WaitWhile(() => relay.listenerState == ConnectionState.Connecting, cancellationToken: cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_relayRetryCts == cts)
                {
                    _relayRetryCts.Dispose();
                    _relayRetryCts = null;
                }
            }
        }

        private bool IsServerListening()
        {
            return NetworkManager.main && NetworkManager.main.transport == _CompositeTransports
                && _CompositeTransports.listenerState is ConnectionState.Connected or ConnectionState.Connecting;
        }

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
            // In process host client, no timeout during long loads
            _CompositeTransports.SetClientTransport(_HostLocalTransport);
        }
        
        public void SetLocal()
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
        
        public bool IsLocal() => NetworkManager.main.transport == _LocalTransport;

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

            if (_HostLocalTransport && _HostLocalTransport.clientState == ConnectionState.Connected)
                return SessionLinkMode.Direct;

            return SessionLinkMode.None;
        }
    }
}
