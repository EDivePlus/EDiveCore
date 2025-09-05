// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using EDIVE.NativeUtils;
using FishNet.Connection;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class ListSceneSpawnPlace : ASceneSpawnPlace   
    {
        [SerializeField]
        private List<Transform> _Locations;

        public override bool TryGetLocation(NetworkConnection conn, out Vector3 position, out Quaternion? rotation)
        {
            position = Vector3.zero;
            rotation = null;
            
            if (_Locations == null || _Locations.Count == 0)
                return false;

            
            var index = conn.ClientId.PositiveModulo(_Locations.Count);
            var location = _Locations[index];
            position = location.position;
            rotation = location.rotation;
            return true;
        }
    }
}
