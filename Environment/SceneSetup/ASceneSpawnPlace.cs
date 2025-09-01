// Author: František Holubec
// Created: 27.08.2025

using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public abstract class ASceneSpawnPlace : MonoBehaviour
    {
        [SerializeField]
        private SceneSetupDefinition _SceneSetup;

        private void Awake()
        {
            _SceneSetup.RegisterSpawnPlace(this);
        }

        public abstract bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation);
    }
}
