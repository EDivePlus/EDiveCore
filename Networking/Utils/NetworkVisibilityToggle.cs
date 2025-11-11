// Author: František Holubec
// Created: 11.11.2025

using EDIVE.StateHandling.ToggleStates;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    public class NetworkVisibilityToggle : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _ToggleState;
        
        private bool _initialVisibilitySet;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _initialVisibilitySet = false;
            NetworkObject.OnHostVisibilityUpdated += OnHostVisibilityUpdated;
        }
        
        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            NetworkObject.OnHostVisibilityUpdated -= OnHostVisibilityUpdated;
        }
        
        private void OnHostVisibilityUpdated(bool prevVisible, bool nextVisible)
        {
            _ToggleState.SetState(nextVisible, !_initialVisibilitySet);
            _initialVisibilitySet = true;
        }
    }
}
