// Author: Radim Holub
// Created: 10.10.2025
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

namespace _Projects.PlayGround.Scripts
{
    public class LightsObserverUpdater : NetworkBehaviour
    {
        private List<Light> _lights = new();
        
        private void Awake()
        {
            _lights = GetComponentsInChildren<Light>().ToList();
            NetworkObject.OnHostVisibilityUpdated += OnHostVisibilityUpdated;
        }
        
        private void OnDestroy()
        {
            NetworkObject.OnHostVisibilityUpdated -= OnHostVisibilityUpdated;
        }

        private void OnHostVisibilityUpdated(bool prevVisible, bool nextVisible)
        {
            _lights.ForEach(l => l.enabled = nextVisible);
        }
    }
}
