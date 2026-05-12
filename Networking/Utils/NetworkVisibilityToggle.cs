// Author: František Holubec
// Created: 11.11.2025

using EDIVE.StateHandling.ToggleStates;
using PurrNet;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    public class NetworkVisibilityToggle : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _ToggleState;

        private bool _initialVisibilitySet;

        // TODO PurrNet migration: FishNet's NetworkObject.OnHostVisibilityUpdated has no direct equivalent.
        // PurrNet exposes visibility via INetworkVisibilityRule / onObserverAdded / onObserverRemoved on NetworkIdentity.
        // Wire those up here once the desired semantics are decided.
        protected override void OnSpawned()
        {
            _initialVisibilitySet = false;
        }

        protected override void OnDespawned()
        {
        }

        private void OnHostVisibilityUpdated(bool prevVisible, bool nextVisible)
        {
            _ToggleState.SetState(nextVisible, !_initialVisibilitySet);
            _initialVisibilitySet = true;
        }
    }
}
