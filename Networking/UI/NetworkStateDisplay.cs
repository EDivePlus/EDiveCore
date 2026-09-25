// Author: František Holubec
// Created: 29.06.2025

using System;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using EDIVE.Networking.ServerManagement.UI;
using EDIVE.StateHandling.MultiStates;
using PurrNet.Transports;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    public class NetworkStateDisplay : MonoBehaviour
    {
        [SerializeField]
        [ValidateMultiState(typeof(ConnectionDisplayState))]
        private AMultiState _ConnectionState;

        [SerializeField]
        [ValidateMultiState(typeof(NetworkRuntimeMode))]
        private AMultiState _RuntimeModeState;
        
        [SerializeField]
        [ValidateMultiState(typeof(SessionLinkMode))]
        private AMultiState _SessionLinkState;
        
        [SerializeField]
        private ServerRecordDisplay _ServerDisplay;

        [SerializeField]
        private TMP_Text _CurrentServerNameText;

        [SerializeField]
        private TMP_Text _ReconnectCountdownText;
        
        private MasterNetworkManager _networkManager;
        private NetworkServerManager _serverManager;
        private IDisposable _serviceRegistration;
        
        private void OnEnable()
        {
            if (!AppCore.Services.IsRegistered<MasterNetworkManager>())
            {
                if (_ConnectionState)
                    _ConnectionState.SetState(ConnectionDisplayState.Disconnected);

                if (_RuntimeModeState)
                    _RuntimeModeState.SetState(NetworkRuntimeMode.None);
            }
            _serviceRegistration = AppCore.Services.WhenRegistered<MasterNetworkManager, NetworkServerManager>(Initialize);
        }

        private void Initialize(MasterNetworkManager networkManager, NetworkServerManager serverManager)
        {
            _networkManager = networkManager;
            _networkManager.ConnectionStateChanged += OnClientConnectionStateChanged;
            _networkManager.RuntimeModeChanged += OnRuntimeModeChanged;
            _serverManager = serverManager;
            _serverManager.ReconnectAttemptFailed += OnReconnectAttemptFailed;
            RefreshState();
        }

        private void OnDisable()
        {
            _serviceRegistration?.Dispose();
            _serviceRegistration = null;
            if (_networkManager)
            {
                _networkManager.ConnectionStateChanged -= OnClientConnectionStateChanged;
                _networkManager.RuntimeModeChanged -= OnRuntimeModeChanged;
            }
            if (_serverManager)
                _serverManager.ReconnectAttemptFailed -= OnReconnectAttemptFailed;
        }

        private void Update()
        {
            if (_ReconnectCountdownText && _serverManager && _serverManager.IsReconnectPending)
                _ReconnectCountdownText.text = Mathf.CeilToInt(_serverManager.ReconnectCountdown).ToString();
        }

        private void OnRuntimeModeChanged(NetworkRuntimeMode networkRuntimeMode) => RefreshState();
        private void OnClientConnectionStateChanged(ConnectionState state) => RefreshState();
        private void OnReconnectAttemptFailed() => RefreshState();

        private void RefreshState()
        {
            if (_ConnectionState)
                _ConnectionState.SetState(ResolveDisplayState());

            if (_RuntimeModeState)
                _RuntimeModeState.SetState(_networkManager.RuntimeMode);
            
            if (_SessionLinkState) 
                UpdateSessionLinkState();

            if (_ServerDisplay)
            {
                _ServerDisplay.Terminate();
                _ServerDisplay.Initialize(_serverManager.CurrentServer);
            }
            
            if(_CurrentServerNameText)
                _CurrentServerNameText.text = _serverManager.CurrentServer?.ServerName ?? "None";
        }

        private ConnectionDisplayState ResolveDisplayState()
        {
            return _networkManager.ConnectionState switch
            {
                ConnectionState.Connecting => ConnectionDisplayState.Connecting,
                ConnectionState.Connected => ConnectionDisplayState.Connected,
                ConnectionState.Disconnecting => ConnectionDisplayState.Disconnecting,
                _ => !_networkManager.ConnectionLost ? ConnectionDisplayState.Disconnected : ResolveLostDisplayState()
            };
        }

        private ConnectionDisplayState ResolveLostDisplayState()
        {
            return _serverManager.ReconnectFailed ? ConnectionDisplayState.ReconnectFailed : ConnectionDisplayState.Reconnecting;
        }

        private void UpdateSessionLinkState()
        {
            if (AppCore.Services.TryGet<TransportController>(out var transports))
                _SessionLinkState.SetState(transports.GetSessionLinkMode());
        }
    }
    
    public enum ConnectionDisplayState
    {
        Connecting,
        Connected,
        Disconnected,
        Disconnecting,
        Reconnecting,
        ReconnectFailed
    }
}
