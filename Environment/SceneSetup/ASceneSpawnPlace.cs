// Author: František Holubec
// Created: 27.08.2025

using EDIVE.Core;
using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public abstract class ASceneSpawnPlace : MonoBehaviour
    {
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

        public abstract bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation);
    }
}
