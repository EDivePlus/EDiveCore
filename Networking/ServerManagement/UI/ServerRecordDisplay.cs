// Author: František Holubec
// Created: 29.04.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Core;
using EDIVE.Time.DateTimeUtils;
using EDIVE.Utils.Activations;
using PurrNet;
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
        private TMP_Text _CurrentPlayersText;
        
        [SerializeField]
        private TMP_Text _MaxPlayersText;

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
                return;
            
            if (_IDText)
                _IDText.text = $"{serverRecord.InstanceID}";
            
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

        private void OnEnable()
        {
            if (_AutoUpdatePlayerCount)
            {
                NetworkManager.main.onPlayerJoined += OnPlayerJoined;
                NetworkManager.main.onPlayerLeft += OnPlayerLeft;
            }
        }

        private void OnDisable()
        {
            if (_AutoUpdatePlayerCount)
            {
                NetworkManager.main.onPlayerJoined -= OnPlayerJoined;
                NetworkManager.main.onPlayerLeft -= OnPlayerLeft;
            }
        }
        
        private void OnPlayerLeft(PlayerID player, bool asServer)
        {
            _CurrentPlayersText.text = $"{Math.Max(0, NetworkManager.main.playerCount)}";
        }

        private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
        {
            _CurrentPlayersText.text = $"{Math.Max(0, NetworkManager.main.playerCount)}";
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
