// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.Core;
using PurrNet;
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
        
        public bool CheckAvailable(SceneSetupDefinition setup)
        {
            return _SetupRestrictions.Count == 0 || _SetupRestrictions.Any(s => s.UniqueID == setup.UniqueID);
        }

        public abstract bool TryGetLocation(PlayerID player, out Vector3 position, out Quaternion? rotation);
    }
}
