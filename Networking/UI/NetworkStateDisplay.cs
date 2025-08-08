// Author: František Holubec
// Created: 29.06.2025

using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    public class NetworkStateDisplay : MonoBehaviour
    {
        [SerializeField]
        [ValidateMultiStateWithEnum(typeof(ConnectionState))]
        private AMultiState _ConnectionState;

        [SerializeField]
        [ValidateMultiStateWithEnum(typeof(NetworkRuntimeMode))]
        private AMultiState _RuntimeModeState;

        [SerializeField]
        private TMP_Text _CurrentServerNameText;
        
        private MasterNetworkManager _networkManager;
        private NetworkServerManager _serverManager;
        
        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<MasterNetworkManager, NetworkServerManager>(Initialize);
        }

        private void Initialize(MasterNetworkManager networkManager, NetworkServerManager serverManager)
        {
            _networkManager = networkManager;
            _networkManager.ConnectionStateChanged.AddListener(OnClientConnectionStateChanged);
            _networkManager.RuntimeModeChanged.AddListener(OnRuntimeModeChanged);
            _serverManager = serverManager;
            RefreshState();
        }

        private void OnDisable()
        {
            if (_networkManager)
            {
                _networkManager.ConnectionStateChanged.RemoveListener(OnClientConnectionStateChanged);
                _networkManager.RuntimeModeChanged.RemoveListener(OnRuntimeModeChanged);
            }
        }

        private void OnRuntimeModeChanged(NetworkRuntimeMode networkRuntimeMode) => RefreshState();
        private void OnClientConnectionStateChanged(ConnectionState state) => RefreshState();

        private void RefreshState()
        {
            if (_ConnectionState)
                _ConnectionState.SetState(_networkManager.ConnectionState);

            if (_RuntimeModeState)
                _RuntimeModeState.SetState(_networkManager.RuntimeMode);

            if (_CurrentServerNameText)
                _CurrentServerNameText.text = _serverManager.ServerConfig.ServerName;
        }
    }
}
