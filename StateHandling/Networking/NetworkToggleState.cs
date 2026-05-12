// Author: František Holubec
// Created: 29.04.2026

using EDIVE.StateHandling.ToggleStates;
using PurrNet;
using UnityEngine;

namespace EDIVE.StateHandling.Networking
{
    public class NetworkToggleState : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _ToggleState;
        
        private readonly SyncVar<bool> _state = new();

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer) return;
            if (_ToggleState == null) return;
            _state.value = _ToggleState.State;
        }

        protected override void OnSpawned()
        {
            if (_ToggleState == null) return;
            _ToggleState.SetState(_state.value, true);
            _ToggleState.StateChanged += OnClientToggleStateChanged;
        }

        protected override void OnDespawned()
        {
            if (_ToggleState == null) return;
            _ToggleState.StateChanged -= OnClientToggleStateChanged;
        }
        
        private void OnClientToggleStateChanged(bool state, bool immediate)
        {
            SetServerToggleState(state, immediate);
        }
        
        [ServerRpc(requireOwnership: false)]
        private void SetServerToggleState(bool state, bool immediate)
        {
            _state.value = state;
            SetObserverToggleState(state, immediate);
        }
        
        [ObserversRpc]
        private void SetObserverToggleState(bool state, bool immediate)
        {
            if (_ToggleState.State == state && !immediate)
                return;
            
            _ToggleState.StateChanged -= OnClientToggleStateChanged;
            _ToggleState.SetState(state, immediate);
            _ToggleState.StateChanged += OnClientToggleStateChanged;
        }
    }
}
