// Author: František Holubec
// Created: 29.04.2026

using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UI
{
    public class ServerEndpointDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private TMP_Text _EndpointText;
        
        [SerializeReference]
        private IActivation _ConnectActivation;
        
        [SerializeField]
        private AToggleState _IsActiveState;
        
        private ServerRecord _serverRecord;
        private AServerEndpoint _serverEndpoint;
        private NetworkServerManager _serverManager;

        public void Initialize(ServerRecord serverRecord, AServerEndpoint serverEndpoint)
        {
            if (serverRecord == null || serverEndpoint == null)
                return;

            _serverRecord = serverRecord;
            _serverEndpoint = serverEndpoint;

            if (_NameText)
                _NameText.text = serverEndpoint.Name;

            if (_EndpointText)
                _EndpointText.text = serverEndpoint.EndpointText;

            _ConnectActivation?.RegisterActivationListener(OnConnectActivated);

            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                _serverManager = serverManager;
                _serverManager.ConnectedEndpointChanged += OnConnectedEndpointChanged;
            }

            SetActive(false);
        }

        public void Terminate()
        {
            _ConnectActivation?.UnregisterActivationListener(OnConnectActivated);
            if (_serverManager != null)
            {
                _serverManager.ConnectedEndpointChanged -= OnConnectedEndpointChanged;
                _serverManager = null;
            }
            _serverRecord = null;
            _serverEndpoint = null;
        }

        public void SetActive(bool active)
        {
            if (_IsActiveState) _IsActiveState.SetState(active);
        }

        private void OnConnectedEndpointChanged(AServerEndpoint endpoint)
        {
            var isActive = _serverManager != null && _serverEndpoint != null && ReferenceEquals(endpoint, _serverEndpoint);
            SetActive(isActive);
        }

        private void OnConnectActivated()
        {
            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                serverManager.ConnectToServer(_serverRecord, _serverEndpoint);
            }
        }
    }
}
