// Author: František Holubec
// Created: 08.10.2025

using System;
using CoordinateSharp;
using UnityEngine;

namespace EDIVE.GeoToolkit.Coordinates
{
    public enum DistanceMeasureAlgorithm
    {
        [Tooltip("Spherical Earth, fast, less accurate")] 
        Haversine,
        
        [Tooltip("Ellipsoidal Earth, slower, more accurate")]
        Vincenty
    }
    
    public static class DistanceMeasureAlgorithmExtensions
    {
        public static Shape ToCoordinateSharpShape(this DistanceMeasureAlgorithm algorithm)
        {
            return algorithm switch
            {
                DistanceMeasureAlgorithm.Haversine => Shape.Sphere,
                DistanceMeasureAlgorithm.Vincenty => Shape.Ellipsoid,
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
            };
        }
    }
}
