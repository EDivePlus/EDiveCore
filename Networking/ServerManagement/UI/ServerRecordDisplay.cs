// Author: František Holubec
// Created: 29.04.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using EDIVE.TimeHandling.DateTimeUtils;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
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
        private TMP_Text _JoinCodeText;

        [SerializeField]
        [ValidateMultiState(typeof(ServerRegistrationState))]
        private AMultiState _RegistrationState;

        [SerializeField]
        private TMP_Text _CurrentPlayersText;
        
        [SerializeField]
        private TMP_Text _MaxPlayersText;

        // Current and max players in one text, e.g. 8 / 32
        [SerializeField]
        private TMP_Text _PlayersText;

        [SerializeField]
        private DateTimeDisplay _LastUpdatedDisplay;
        
        [SerializeReference]
        private IActivation _ConnectActivation;
        
        [SerializeField]
        private List<ServerEndpointDisplay> _EndpointDisplays;
        
        [PropertySpace]
        [SerializeField]
        [PropertyTooltip("Automatically update the player count based on the current connected to server.")]
        private bool _AutoUpdatePlayerCount;

        private ServerRecord _serverRecord;
        
        public void Initialize(ServerRecord serverRecord)
        {
            _serverRecord = serverRecord;
            if (_serverRecord == null)
            {
                ClearDisplay();
                return;
            }
            
            _ConnectActivation?.Register(OnConnectActivated);
            
            _serverRecord.StateChanged += OnServerRecordChanged;
            
            UpdateDisplay();
        }

        public void Terminate()
        {
            foreach (var endpointDisplay in _EndpointDisplays.Where(d => d != null))
            {
                endpointDisplay.SetActive(false);
            }
            _ConnectActivation?.Unregister(OnConnectActivated);
            
            if (_serverRecord != null)
                _serverRecord.StateChanged -= OnServerRecordChanged;
            
            _serverRecord = null;
        }

        private void UpdateDisplay()
        {
            if (_IDText)
                _IDText.text = $"{_serverRecord.InstanceID}";
            
            if (_NameText)
                _NameText.text = _serverRecord.ServerName;

            if (_JoinCodeText)
                _JoinCodeText.text = _serverRecord.JoinCode ?? "-";

            if (_RegistrationState)
                _RegistrationState.SetState(_serverRecord.RegistrationState);

            if (_CurrentPlayersText)
                _CurrentPlayersText.text = $"{_serverRecord.CurrentPlayers}";
            
            if (_MaxPlayersText)
                _MaxPlayersText.text = $"{_serverRecord.MaxPlayers}";

            if (_PlayersText)
                _PlayersText.text = $"{_serverRecord.CurrentPlayers} / {_serverRecord.MaxPlayers}";
            
            if (_LastUpdatedDisplay)
                _LastUpdatedDisplay.SetDateTime(_serverRecord.LastUpdated);

            var endpoints = _serverRecord.Endpoints;
            var validDisplays = _EndpointDisplays.Where(d => d != null).ToList();
            for (var i = 0; i < validDisplays.Count; i++)
            {
                var endpointDisplay = validDisplays[i];
                if (i < endpoints.Count)
                {
                    endpointDisplay.gameObject.SetActive(true);
                    endpointDisplay.SetEndpoint(endpoints[i]);
                }
                else
                {
                    endpointDisplay.gameObject.SetActive(false);
                }
            }
        }
        
        private void ClearDisplay()
        {
            if (_IDText)
                _IDText.text = "-";

            if (_NameText)
                _NameText.text = "-";

            if (_JoinCodeText)
                _JoinCodeText.text = "-";

            if (_CurrentPlayersText)
                _CurrentPlayersText.text = "-";

            if (_MaxPlayersText)
                _MaxPlayersText.text = "-";

            if (_PlayersText)
                _PlayersText.text = "-";

            foreach (var endpointDisplay in _EndpointDisplays.Where(d => d != null))
            {
                endpointDisplay.gameObject.SetActive(false);
            }
        }

        private void OnServerRecordChanged(ServerRecord record)
        {
            if (record != _serverRecord)
                return;
            UpdateDisplay();
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
