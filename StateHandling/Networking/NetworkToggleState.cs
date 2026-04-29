// Author: František Holubec
// Created: 29.04.2026

using EDIVE.StateHandling.ToggleStates;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace EDIVE.StateHandling.Networking
{
    public class NetworkToggleState : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _ToggleState;
        
        private readonly SyncVar<bool> _state = new();

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_ToggleState == null)
                return;
            
            _state.Value = _ToggleState.State;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_ToggleState == null)
                return;
            _ToggleState.SetState(_state.Value, true);
            _ToggleState.StateChanged += OnClientToggleStateChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _ToggleState.StateChanged -= OnClientToggleStateChanged;
        }
        
        private void OnClientToggleStateChanged(bool state, bool immediate)
        {
            SetServerToggleState(state, immediate);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetServerToggleState(bool state, bool immediate)
        {
            _state.Value = state;
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
