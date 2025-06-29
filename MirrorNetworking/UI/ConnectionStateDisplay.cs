// Author: František Holubec
// Created: 29.06.2025

using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;

namespace EDIVE.MirrorNetworking.UI
{
    public class ConnectionStateDisplay : MonoBehaviour
    {
        [SerializeField]
        private AToggleState _ConnectedState;
        
        [SerializeField]
        private AMultiState _NetworkModeState;
        
        private MasterNetworkManager _networkManager;
        
        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<MasterNetworkManager>(Initialize);
        }

        private void OnDisable()
        {
            if (_networkManager)
                _networkManager.RuntimeStateChanged.RemoveListener(OnRuntimeStateChanged);
        }

        private void Initialize(MasterNetworkManager networkManager)
        {
            _networkManager = networkManager;
            if (_ConnectedState) 
                _ConnectedState.SetState(_networkManager.isNetworkActive, true);

            if (_NetworkModeState)
                _NetworkModeState.SetState(_networkManager.CurrentNetworkMode, true);
            
            _networkManager.RuntimeStateChanged.AddListener(OnRuntimeStateChanged);
        }

        private void OnRuntimeStateChanged(bool state, NetworkRuntimeMode mode)
        {
            if (_ConnectedState) 
                _ConnectedState.SetState(_networkManager.isNetworkActive);

            if (_NetworkModeState)
                _NetworkModeState.SetState(_networkManager.CurrentNetworkMode);
        }
    }
}
