// Author: Michal Petr
// Created: 06.05.2026

#if PURRNET
using PurrNet;
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
        private string ShareToken => _shareToken.value;

        private void Awake()
        {
            _handler = GetComponent<ARemoteContentHandler>();
        }

        protected override void OnSpawned()
        {
            _handler.ShareTokenChanged += OnHandlerShareTokenChanged;
            _shareToken.onChanged += OnSyncShareTokenChanged;

            if (!string.IsNullOrEmpty(_handler.ShareToken))
            {
                if (isServer)
                    _shareToken.value = _handler.ShareToken;
                else
                    ServerSetShareToken(_handler.ShareToken);
            }
        }

        protected override void OnDespawned()
        {
            _handler.ShareTokenChanged -= OnHandlerShareTokenChanged;
            _shareToken.onChanged -= OnSyncShareTokenChanged;
        }

        private void OnSyncShareTokenChanged(string next)
        {
            if (string.IsNullOrEmpty(next) || _handler == null)
                return;
            _handler.SetShareToken(next);
        }

        private void OnHandlerShareTokenChanged(string newToken)
        {
            if (isServer)
                _shareToken.value = newToken;
            else
                ServerSetShareToken(newToken);
        }

        [ServerRpc(requireOwnership: false)]
        private void ServerSetShareToken(string newToken)
        {
            _shareToken.value = newToken;
        }
    }
}
#endif