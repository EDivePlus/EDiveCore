// Author: František Holubec
// Created: 20.10.2026

using PurrNet;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Displays
{
    // Shares a StreamScreen with the whole session, the host applies ServiceHub commands
    [RequireComponent(typeof(StreamScreen))]
    public class NetworkStreamScreen : NetworkBehaviour
    {
        private readonly SyncVar<StreamScreenState> _state = new(StreamScreenState.Empty);

        private StreamScreen _screen;
        private bool _listening;

        private void Awake()
        {
            _screen = GetComponent<StreamScreen>();
        }

        protected override void OnSpawned(bool asServer)
        {
            // Host spawns as server and client, the client side wants video
            if (!_listening)
                _state.onChanged += OnStateChanged;
            _listening = true;
            OnStateChanged(_state.value);
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!_listening || (asServer && isClient))
                return;
            _listening = false;
            _state.onChanged -= OnStateChanged;
            _screen.SetState(StreamScreenState.Empty);
        }

        public bool ServerSetState(StreamScreenState state)
        {
            if (!isServer)
                return false;
            _state.value = state ?? StreamScreenState.Empty;
            return true;
        }

        private void OnStateChanged(StreamScreenState state)
        {
            _screen.SetState(state);
        }
    }
}
