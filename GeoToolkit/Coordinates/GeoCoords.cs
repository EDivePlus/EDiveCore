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
        [InlineIconButton(FontAwesomeEditorIconType.LocationCrosshairsSolid, "OpenInMaps", "Open in maps", GUIAlwaysEnabled = true)]
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
        
        public bool TryConvertTo(CoordinateSystemType targetSystem, out GeoCoords result)
        {
            var success = TryConvert(_Position, _CoordinateSystem, targetSystem, out var position);
            result = new GeoCoords(position, targetSystem);
            return success;
        }

        public double DistanceTo(GeoCoords other, DistanceMeasureAlgorithm alg)
        {
            const CoordinateSystemType conversionSystem = CoordinateSystemType.EPSG_4326;
            if (!TryConvertTo(conversionSystem, out var a) || !other.TryConvertTo(conversionSystem, out var b))
                return double.NaN;
            return Distance(a.Position, b.Position, conversionSystem, alg);
        }
        
        public static double2 ConvertTo(double2 pos, string sourceSystem, string targetSystem)
        {
            return Convert(pos, CoordinateSystemTypeUtility.Parse(sourceSystem), CoordinateSystemTypeUtility.Parse(targetSystem));
        }

        // NaN on failure, so bad result is visible
        public static double2 Convert(double2 pos, CoordinateSystemType sourceSystemType, CoordinateSystemType targetSystemType)
        {
            return TryConvert(pos, sourceSystemType, targetSystemType, out var result) ? result : new double2(double.NaN, double.NaN);
        }

        public static bool TryConvert(double2 pos, CoordinateSystemType sourceSystemType, CoordinateSystemType targetSystemType, out double2 result)
        {
            result = pos;
            if (sourceSystemType == targetSystemType)
                return true;

            try
            {
                var ctFact = new CoordinateTransformationFactory();
                var transformation = ctFact.CreateFromCoordinateSystems(sourceSystemType.GetCoordinateSystem(), targetSystemType.GetCoordinateSystem());
                var transformed = transformation.MathTransform.Transform(new[] {pos.x, pos.y});
                result = new double2(transformed[0], transformed[1]);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeoCoords] Convert {sourceSystemType} -> {targetSystemType} failed: {e.Message}");
                result = new double2(double.NaN, double.NaN);
                return false;
            }
        }
        
        // NaN on failure
        public static double Distance(double2 posA, double2 posB, CoordinateSystemType targetSystem, DistanceMeasureAlgorithm alg)
        {
            // Calculate raw distance if coordinate system is unknown
            if (targetSystem == CoordinateSystemType.Unknown)
                return math.distance(posA, posB);
            
            // Convert to WGS84 for the CoordinateSharp library
            if (targetSystem != CoordinateSystemType.EPSG_4326)
            {
                if (!TryConvert(posA, targetSystem, CoordinateSystemType.EPSG_4326, out posA) ||
                    !TryConvert(posB, targetSystem, CoordinateSystemType.EPSG_4326, out posB))
                    return double.NaN;
            }

            try
            {
                var eagerLoad = new EagerLoad(false);
                var coordA = new Coordinate(posA.y, posA.x, eagerLoad);
                var coordB = new Coordinate(posB.y, posB.x, eagerLoad);
                var dist = new Distance(coordA, coordB, alg.ToCoordinateSharpShape());
                return dist.Meters;   
            }
            catch (Exception e)
            {
                Debug.LogError($"[GeoCoords] Distance failed: {e.Message}");
                return double.NaN;
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void CopyCoords(Rect fieldRect) => CoordinatesClipboardUtility.ShowCopyDropdown(this, fieldRect);

        [UsedImplicitly]
        private void OpenInMaps() => GeoJsonPreviewUtility.OpenPoint(this);
#endif
    }
}
