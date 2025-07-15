// Author: František Holubec
// Created: 29.06.2025

using EDIVE.Core;
using EDIVE.MirrorNetworking.ServerManagement;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine;

namespace EDIVE.MirrorNetworking.UI
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
            _networkManager.ClientConnectionStateChanged.AddListener(OnClientConnectionStateChanged);
            _networkManager.RuntimeStateChanged.AddListener(OnRuntimeStateChanged);
            
            _serverManager = serverManager;
            RefreshState();
        }

        private void OnDisable()
        {
            if (_networkManager)
            {
                _networkManager.RuntimeStateChanged.RemoveListener(OnRuntimeStateChanged);
            }
        }

        private void OnRuntimeStateChanged(bool state, NetworkRuntimeMode mode) => RefreshState();
        private void OnClientConnectionStateChanged(ConnectionState state) => RefreshState();

        private void RefreshState()
        {
            if (_ConnectionState)
                _ConnectionState.SetState(_networkManager.ConnectionState);

            if (_RuntimeModeState)
                _RuntimeModeState.SetState(_networkManager.CurrentNetworkMode);

            if (_CurrentServerNameText)
                _CurrentServerNameText.text = _serverManager.ServerConfig.ServerName;
        }
    }
}
