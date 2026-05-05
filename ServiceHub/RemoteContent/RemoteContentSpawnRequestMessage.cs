// Author: Michal Petr
// Created: 05.05.2026

using FishNet.Broadcast;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    public struct RemoteContentSpawnRequestMessage : IBroadcast
    {
        public int PrefabIndex;
        public string ContentId;
        public string SceneName;
        public Vector3 Position;
        public Quaternion Rotation;
    }
}
