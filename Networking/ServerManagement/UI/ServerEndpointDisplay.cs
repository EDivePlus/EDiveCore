// Author: František Holubec
// Created: 29.04.2026

using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
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
        
        [SerializeField]
        private AToggleState _IsActiveState;
        
        private AServerEndpoint _serverEndpoint;
        private NetworkServerManager _serverManager;

        private void OnEnable()
        {
            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                _serverManager = serverManager;
                _serverManager.ConnectedEndpointChanged += OnConnectedEndpointChanged;
            }
        }

        private void OnDisable()
        {
            if (_serverManager != null)
            {
                _serverManager.ConnectedEndpointChanged -= OnConnectedEndpointChanged;
                _serverManager = null;
            }
        }

        public void SetEndpoint(AServerEndpoint serverEndpoint)
        {
            if (serverEndpoint == null)
                return;
            
            _serverEndpoint = serverEndpoint;
            
            SetActive(false);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_serverEndpoint == null)
                return;

            if (_NameText)
                _NameText.text = _serverEndpoint.Name;
            
            if (_EndpointText)
                _EndpointText.text = _serverEndpoint.EndpointText;
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
    }
}
