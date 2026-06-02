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
    }
}
