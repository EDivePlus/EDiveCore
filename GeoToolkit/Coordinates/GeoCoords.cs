// Author: František Holubec
// Created: 17.11.2025

using System;
using CoordinateSharp;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using ProjNet.CoordinateSystems.Transformations;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Coordinates
{
    [Serializable]
    public struct GeoCoords
    {
        [SerializeField]
        [InlineIconButton(FontAwesomeEditorIconType.CopySolid, "CopyCoords", "Copy coordinates", GUIAlwaysEnabled = true)]
        private double2 _Position;
        
        [SerializeField]
        private CoordinateSystemType _CoordinateSystem;

        public double2 Position => _Position;
        public CoordinateSystemType CoordinateSystem => _CoordinateSystem;

        public GeoCoords(double2 position, CoordinateSystemType coordinateSystem)
        {
            _Position = position;
            _CoordinateSystem = coordinateSystem;
        }

        public GeoCoords ConvertTo(CoordinateSystemType targetSystem)
        {
            return new GeoCoords(Convert(_Position, _CoordinateSystem, targetSystem), targetSystem);
        }
        
        public double DistanceTo(GeoCoords other, DistanceMeasureAlgorithm alg)
        {
            const CoordinateSystemType conversionSystem = CoordinateSystemType.EPSG_4326;
            var posA = ConvertTo(conversionSystem).Position;
            var posB = other.ConvertTo(conversionSystem).Position;
            return Distance(posA, posB, conversionSystem, alg);
        }
        
        public static double2 ConvertTo(double2 pos, string sourceSystem, string targetSystem)
        {
            return Convert(pos, CoordinateSystemTypeUtility.Parse(sourceSystem), CoordinateSystemTypeUtility.Parse(targetSystem));
        }

        public static double2 Convert(double2 pos, CoordinateSystemType sourceSystemType, CoordinateSystemType targetSystemType)
        {
            if (sourceSystemType == targetSystemType)
                return pos;
            
            var ctFact = new CoordinateTransformationFactory();
            var transformation = ctFact.CreateFromCoordinateSystems(sourceSystemType.GetCoordinateSystem(), targetSystemType.GetCoordinateSystem());
            var result = transformation.MathTransform.Transform(new []{ pos.x, pos.y });
            return new double2(result[0], result[1]);
        }
        
        public static double Distance(double2 posA, double2 posB, CoordinateSystemType targetSystem, DistanceMeasureAlgorithm alg)
        {
            // Calculate raw distance if coordinate system is unknown
            if (targetSystem == CoordinateSystemType.Unknown)
                return math.distance(posA, posB);
            
            // Convert to WGS84 for the CoordinateSharp library
            if (targetSystem != CoordinateSystemType.EPSG_4326)
            {
                posA = Convert(posA, targetSystem, CoordinateSystemType.EPSG_4326);
                posB = Convert(posB, targetSystem, CoordinateSystemType.EPSG_4326);
            }
            var eagerLoad = new EagerLoad(false);
            var coordA = new Coordinate(posA.y, posA.x, eagerLoad);
            var coordB = new Coordinate(posB.y, posB.x, eagerLoad);
            var dist = new Distance(coordA, coordB, alg.ToCoordinateSharpShape());
            return dist.Meters;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void CopyCoords(Rect fieldRect) => CoordinatesClipboardUtility.ShowCopyDropdown(this, fieldRect);
#endif
    }
}
