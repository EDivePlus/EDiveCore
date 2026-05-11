// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using EDIVE.Core;
using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public abstract class ASceneSpawnPlace : MonoBehaviour
    {
        [SerializeField]
        private List<SceneSetupDefinition> _SetupRestrictions = new();
        
        private void OnEnable()
        {
            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneSetupManager))
            {
                sceneSetupManager.RegisterSpawnPlace(this);
            }
        }

        private void OnDisable()
        {
            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneSetupManager))
            {
                sceneSetupManager.UnregisterSpawnPlace(this);
            }
        }

        
        public bool CheckAvailable(SceneSetupDefinition setup) => _SetupRestrictions.Count == 0 || _SetupRestrictions.Contains(setup);
        
        public abstract bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation);
    }
}
