// Author: František Holubec
// Created: 27.08.2025

using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Environment.SceneContext
{
    public abstract class ASceneContextSpawnPlace : MonoBehaviour
    {
        [SerializeField]
        private SceneContextDefinition _SceneContext;

        private void Awake()
        {
            _SceneContext.RegisterSpawnPlace(this);
        }

        public abstract bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation);
    }
}
