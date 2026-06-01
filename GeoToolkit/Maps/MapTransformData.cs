// Author: František Holubec
// Created: 17.11.2025

using Unity.Mathematics;

namespace EDIVE.GeoToolkit.Maps
{
    public readonly struct MapTransformData
    {
        public readonly float3 Origin;
        public readonly float3 Max;
        public readonly float3 AxisX; // Right (Map X)
        public readonly float3 AxisY; // Up (Map Normal)
        public readonly float3 AxisZ; // Forward (Map Y)

        public readonly float3 AxisXNormalized;
        public readonly float3 AxisYNormalized;
        public readonly float3 AxisZNormalized;

        public readonly float3 Size;
        public readonly quaternion Rotation;

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
