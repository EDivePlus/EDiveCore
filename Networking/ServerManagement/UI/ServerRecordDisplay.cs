// Author: František Holubec
// Created: 29.04.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.Core;
using EDIVE.Time.DateTimeUtils;
using EDIVE.Utils.Activations;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UI
{
    public class ServerRecordDisplay : MonoBehaviour
    {    
        [SerializeField]
        private TMP_Text _IDText;
        
        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private TMP_Text _CurrentPlayersText;
        
        [SerializeField]
        private TMP_Text _MaxPlayersText;

        [SerializeField]
        private DateTimeDisplay _LastUpdatedDisplay;
        
        [SerializeReference]
        private IActivation _ConnectActivation;
        
        [SerializeField]
        private List<ServerEndpointDisplay> _EndpointDisplays;

        private ServerRecord _serverRecord;
        
        public void Initialize(ServerRecord serverRecord)
        {
            _serverRecord = serverRecord;
            if (_serverRecord == null)
                return;
            
            if (_IDText)
                _IDText.text = $"{serverRecord.ServerID}";
            
            if(_NameText)
                _NameText.text = serverRecord.ServerName;
            
            if (_CurrentPlayersText)
                _CurrentPlayersText.text = $"{serverRecord.CurrentPlayers}";
            
            if (_MaxPlayersText)
                _MaxPlayersText.text = $"{serverRecord.MaxPlayers}";
            
            if (_LastUpdatedDisplay)
                _LastUpdatedDisplay.SetDateTime(serverRecord.LastUpdated);

            var endpoints = _serverRecord.Endpoints;
            var validDisplays = _EndpointDisplays.Where(d => d != null).ToList();
            for (var i = 0; i < validDisplays.Count; i++)
            {
                var endpointDisplay = validDisplays[i];
                endpointDisplay.Terminate();
                if (i < endpoints.Count)
                {
                    endpointDisplay.gameObject.SetActive(true);
                    endpointDisplay.Initialize(_serverRecord, endpoints[i]);
                }
                else
                {
                    endpointDisplay.gameObject.SetActive(false);
                }
            }
            _ConnectActivation?.RegisterActivationListener(OnConnectActivated);
        }

        public void Terminate()
        {
            foreach (var endpointDisplay in _EndpointDisplays.Where(d => d != null))
            {
                endpointDisplay.Terminate();
            }
            _ConnectActivation?.UnregisterActivationListener(OnConnectActivated);
            _serverRecord = null;
        }
        
        private void OnConnectActivated()
        {
            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                serverManager.ConnectToServer(_serverRecord);
            }
        }
    }
}
