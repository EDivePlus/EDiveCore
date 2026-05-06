// Author: Michal Petr
// Created: 06.05.2026

using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent.Handlers
{
    [RequireComponent(typeof(ARemoteContentHandler))]
    public class NetworkRemoteContentHandlerSynchronizer : NetworkBehaviour
    {
        private ARemoteContentHandler _handler;
        private readonly SyncVar<string> _shareToken = new();
        
        [ShowInInspector]
        private string ShareToken => _shareToken.Value;

        private void Awake()
        {
            _handler = GetComponent<ARemoteContentHandler>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _handler.ShareTokenChanged += OnHandlerShareTokenChanged;
            _shareToken.OnChange += OnSyncShareTokenChanged;
            
            if (!string.IsNullOrEmpty(_handler.ShareToken))
            {
                if (IsServerInitialized)
                    _shareToken.Value = _handler.ShareToken;
                else
                    ServerSetShareToken(_handler.ShareToken);
            }
        }

        public override void OnStopNetwork()
        {
            _handler.ShareTokenChanged -= OnHandlerShareTokenChanged;
            _shareToken.OnChange -= OnSyncShareTokenChanged;
            base.OnStopNetwork();
        }

        private void OnSyncShareTokenChanged(string prev, string next, bool asServer)
        {
            if (string.IsNullOrEmpty(next) || _handler == null)
                return;
            _handler.SetShareToken(next);
        }
        
        private void OnHandlerShareTokenChanged(string newToken)
        {
            if (IsServerInitialized)
                _shareToken.Value = newToken;
            else
                ServerSetShareToken(newToken);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void ServerSetShareToken(string newToken)
        {
            _shareToken.Value = newToken;
        }
    }
}
