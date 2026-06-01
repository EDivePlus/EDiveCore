using System;
using System.Globalization;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.GeoToolkit.Area
{
    [Serializable]
    public struct GeoAreaRect
    {
        [SerializeField]
        private CoordinateSystemType _CoordinateSystem;

        [SerializeField]
        private double2 _Min;

        [SerializeField]
        private double2 _Max;

        [PropertySpace]
        [SerializeField]
        [InlineIconButton("Refresh", "RecalculateAreaSize")]
        private double2 _GeoSize;

        public GeoAreaRect(double2 min, double2 max, CoordinateSystemType coordinateSystem)
        {
            _Min = min;
            _Max = max;
            _CoordinateSystem = coordinateSystem;
            _GeoSize = double2.zero;
        }

        public CoordinateSystemType CoordinateSystem => _CoordinateSystem;
        public double2 GeoSize => _GeoSize;

        public double2 Min => _Min;
        public double2 Max => _Max;
        public double2 Size => _Max - _Min;
        
        public GeoCoords MinCoords => new(_Min, CoordinateSystem);
        public GeoCoords MaxCoords => new(_Max, CoordinateSystem);
        
        public string ToCommaSeparatedString()
        {
            return $"({Min.x.ToString(CultureInfo.InvariantCulture)}," +
                   $"{Min.y.ToString(CultureInfo.InvariantCulture)})," +
                   $"({Max.x.ToString(CultureInfo.InvariantCulture)}," +
                   $"{Max.y.ToString(CultureInfo.InvariantCulture)})";
        }
        
        public double2 InverseLerp(GeoCoords geoCoord)
        {
            var pos = geoCoord.ConvertTo(CoordinateSystem).Position;
            var inverseRange = 1 / Size;
            var u = (pos.x - Min.x) * inverseRange.x;
            var v = (pos.y - Min.y) * inverseRange.y;
            return new double2(u, v);
        }

        public GeoCoords Lerp(double2 relativePos)
        {
            var geoX = Min.x + relativePos.x * (Max.x - Min.x);
            var geoY = Min.y + relativePos.y * (Max.y - Min.y);
            return new GeoCoords(new double2(geoX, geoY), CoordinateSystem);
        }
        
#if UNITY_EDITOR
        [UsedImplicitly]
        public void RecalculateAreaSize(InspectorProperty property)
        {
            var origin = new GeoCoords(new double2(Min.x, Min.y), CoordinateSystem);
            var xMax = new GeoCoords(new double2(Max.x, Min.y), CoordinateSystem);
            var yMax = new GeoCoords(new double2(Min.x, Max.y), CoordinateSystem);
            var xSize = origin.DistanceTo(xMax, DistanceMeasureAlgorithm.Vincenty);
            var ySize = origin.DistanceTo(yMax, DistanceMeasureAlgorithm.Vincenty);
            _GeoSize = new double2(xSize, ySize);
            property.MarkSerializationRootDirty();
        }
#endif
    }
}