// Author: Radim Holub
// Created: 15.01.2026

using FishNet.Broadcast;
using UnityEngine;

namespace EDIVE.Networking.Players
{
    public struct SummonPlayersToMeRequest : IBroadcast
    {
        public readonly string SceneName;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float Radius;

        public SummonPlayersToMeRequest(string sceneName, Vector3 position, Quaternion rotation, float radius = 0.75f)
        {
            SceneName = sceneName;
            Position = position;
            Rotation = rotation;
            Radius = radius;
        }
        
    }
}

