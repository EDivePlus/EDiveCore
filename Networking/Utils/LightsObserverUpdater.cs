// Author: Radim Holub
// Created: 10.10.2025

using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    public class LightsObserverUpdater : NetworkBehaviour
    {
        private List<Light> _lights = new();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _lights = GetComponentsInChildren<Light>().ToList();
            NetworkObject.OnHostVisibilityUpdated += OnHostVisibilityUpdated;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            NetworkObject.OnHostVisibilityUpdated -= OnHostVisibilityUpdated;
        }

        private void OnHostVisibilityUpdated(bool prevVisible, bool nextVisible)
        {
            _lights.ForEach(l => l.enabled = nextVisible);
        }
    }
}
