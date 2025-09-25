// Author: František Holubec
// Created: 25.09.2025

using UnityEngine;

namespace EDIVE.EyeTracking
{
    public struct Pose
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static readonly Pose IDENTITY = new(Vector3.zero, Quaternion.identity);

        public Pose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        
        public readonly Pose ToWorldSpace(Pose cameraPose)
        {
            var worldPos = cameraPose.Position + cameraPose.Rotation * Position;
            var worldRot = cameraPose.Rotation * Rotation;
            return new Pose(worldPos, worldRot);
        }
    }
}
