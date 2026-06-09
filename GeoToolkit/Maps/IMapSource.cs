// Author: František Holubec
// Created: 17.11.2025

using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    [EnhancedTypeSelector(true, 1)]
    public interface IMapSource
    {
        public bool IsValid { get; }
        public MapTransformData CalculateTransformData(Transform mapTransform);
    }
}
