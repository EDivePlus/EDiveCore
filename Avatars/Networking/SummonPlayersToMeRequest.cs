// Author: Radim Holub
// Created: 15.01.2026

using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public struct SummonPlayersToMeRequest : IPackedAuto
    {
        public string SceneName;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Radius;

        public SummonPlayersToMeRequest(string sceneName, Vector3 position, Quaternion rotation, float radius = 0.75f)
        {
            SceneName = sceneName;
            Position = position;
            Rotation = rotation;
            Radius = radius;
        }
    }
}
