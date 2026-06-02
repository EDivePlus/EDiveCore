// Author: František Holubec
// Created: 17.11.2025

using System;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    [Serializable]
    public class CustomMapSource : IMapSource
    {
        [SerializeField]
        private Vector3 _BoundsMin;

        [SerializeField]
        private Vector3 _BoundsMax = Vector3.one;

        public bool IsValid => true;

        public MapTransformData CalculateTransformData(Transform mapTransform)
        {
            // Bounds are expressed in mapTransform's local space.
            return MapTransformData.FromLocalBounds(mapTransform, (float3) _BoundsMin, (float3) _BoundsMax);
        }
    }
}
