// Author: František Holubec
// Created: 17.11.2025

using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    public struct MapTransformData
    {
        public float3 Origin;
        public float3 Max;
        public float3 AxisX; // Right (Map X)
        public float3 AxisY; // Up (Map Normal)
        public float3 AxisZ; // Forward (Map Y)

        [HideInInspector]
        public float3 AxisXNormalized;
        [HideInInspector]
        public float3 AxisYNormalized;
        [HideInInspector]
        public float3 AxisZNormalized;

        [HideInInspector]
        public float3 Size;
        [HideInInspector]
        public quaternion Rotation;

        public MapTransformData(float3 origin, float3 axisX, float3 axisY, float3 axisZ) : this()
        {
            Origin = origin;
            AxisX = axisX;
            AxisY = axisY;
            AxisZ = axisZ;
            AxisXNormalized = math.normalize(axisX);
            AxisYNormalized = math.normalize(axisY);
            AxisZNormalized = math.normalize(axisZ);
            Max = origin + axisX + axisY + axisZ;
            Size = new float3(math.length(axisX), math.length(axisY), math.length(axisZ));
            Rotation = quaternion.LookRotation(math.normalize(axisZ), math.normalize(axisY));
        }

        /// <summary>
        /// Builds transform data from an axis-aligned box defined in <paramref name="mapTransform"/>'s local space,
        /// mapping each local extent onto the corresponding world-space axis of the transform.
        /// </summary>
        public static MapTransformData FromLocalBounds(Transform mapTransform, float3 localMin, float3 localMax)
        {
            var localSize = localMax - localMin;
            var localAxisX = new float3(localSize.x, 0f, 0f);
            var localAxisY = new float3(0f, localSize.y, 0f);
            var localAxisZ = new float3(0f, 0f, localSize.z);

            var worldOrigin = (float3) mapTransform.TransformPoint(localMin);
            var worldAxisX = (float3) mapTransform.TransformVector(localAxisX);
            var worldAxisY = (float3) mapTransform.TransformVector(localAxisY);
            var worldAxisZ = (float3) mapTransform.TransformVector(localAxisZ);

            return new MapTransformData(worldOrigin, worldAxisX, worldAxisY, worldAxisZ);
        }
    }
}
