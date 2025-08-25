// Author: František Holubec
// Created: 21.08.2025

using EDIVE.Core.Services;
using EDIVE.Utils;
using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Networking.Spawning
{
    public abstract class APlayerSpawnPlace : ABaseServiceBehaviour<APlayerSpawnPlace>
    {
        public abstract bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation);

        private void OnEnable()
        {
            gameObject.AddParentSceneActiveChangeListener(OnParentSceneActiveChanged);
        }
        
        private void OnDisable()
        {
            gameObject.RemoveParentSceneActiveChangeListener(OnParentSceneActiveChanged);
        }

        private void OnParentSceneActiveChanged(bool active)
        {
            if (active)
                RegisterService();
            else
                UnregisterService();
        }
    }
}
