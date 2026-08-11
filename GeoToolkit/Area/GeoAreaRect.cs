using System;
using System.Globalization;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.OdinExtensions;
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
        [InlineIconButton(FontAwesomeEditorIconType.CopySolid, "CopyCoords", "Copy coordinates", GUIAlwaysEnabled = true)]
        private double2 _Min;

        [SerializeField]
        [InlineIconButton(FontAwesomeEditorIconType.CopySolid, "CopyCoords", "Copy coordinates", GUIAlwaysEnabled = true)]
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

        public GeoAreaRect(double2 min, double2 max, CoordinateSystemType coordinateSystem, double2 geoSize)
        {
            _Min = min;
            _Max = max;
            _CoordinateSystem = coordinateSystem;
            _GeoSize = geoSize;
        }

        public CoordinateSystemType CoordinateSystem => _CoordinateSystem;
        public double2 GeoSize => _GeoSize;

        public double2 Min => _Min;
        public double2 Max => _Max;
        public double2 Size => _Max - _Min;
        
        public GeoCoords MinCoords => new(_Min, CoordinateSystem);
        public GeoCoords MaxCoords => new(_Max, CoordinateSystem);
        
        public string ToCommaSeparatedString(bool swapAxisOrder = false)
        {
            var min = swapAxisOrder ? Min.yx : Min.xy;
            var max = swapAxisOrder ? Max.yx : Max.xy;
            return $"{min.x.ToString(CultureInfo.InvariantCulture)}," +
                   $"{min.y.ToString(CultureInfo.InvariantCulture)}," +
                   $"{max.x.ToString(CultureInfo.InvariantCulture)}," +
                   $"{max.y.ToString(CultureInfo.InvariantCulture)}";
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

        public double2 CalculateGeoSize()
        {
            var origin = new GeoCoords(new double2(Min.x, Min.y), CoordinateSystem);
            var xMax = new GeoCoords(new double2(Max.x, Min.y), CoordinateSystem);
            var yMax = new GeoCoords(new double2(Min.x, Max.y), CoordinateSystem);
            var xSize = origin.DistanceTo(xMax, DistanceMeasureAlgorithm.Vincenty);
            var ySize = origin.DistanceTo(yMax, DistanceMeasureAlgorithm.Vincenty);
            return new double2(xSize, ySize);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        public void RecalculateAreaSize(InspectorProperty property)
        {
            _GeoSize = CalculateGeoSize();
            property.MarkSerializationRootDirty();
        }

        [UsedImplicitly]
        private void CopyCoords(double2 value, Rect fieldRect) => CoordinatesClipboardUtility.ShowCopyDropdown(new GeoCoords(value, CoordinateSystem), fieldRect);
        
#endif
    }
}