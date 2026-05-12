// Author: František Holubec
// Created: 29.04.2026

using EDIVE.Core;
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
        
        private ServerRecord _serverRecord;
        private AServerEndpoint _serverEndpoint;
        
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
        }

        public void Terminate()
        {
            _ConnectActivation?.UnregisterActivationListener(OnConnectActivated);
            _serverRecord = null;
            _serverEndpoint = null;
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
