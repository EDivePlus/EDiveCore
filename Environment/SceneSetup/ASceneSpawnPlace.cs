// Author: František Holubec
// Created: 27.08.2025

using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public abstract class ASceneSpawnPlace : MonoBehaviour
    {
        public abstract bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation);
    }
}
