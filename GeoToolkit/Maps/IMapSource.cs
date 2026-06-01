// Author: František Holubec
// Created: 17.11.2025

using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    public interface IMapSource
    {
        /// <summary>
        /// True when the source has enough valid data to produce a meaningful <see cref="MapTransformData"/>.
        /// </summary>
        public bool IsValid { get; }

        public MapTransformData CalculateTransformData(Transform mapTransform);
    }
}
