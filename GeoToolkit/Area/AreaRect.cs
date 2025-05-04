using System;
using System.Globalization;
using CoordinateSharp;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using ProtoGIS.Scripts.Utils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.Area
{
    [Serializable]
    [InlineProperty]
    public class AreaRect : IAreaRect
    {
        [FormerlySerializedAs("_CoordRefSystem")]
        [SerializeField]
        private CoordinateSystemType _CoordinateSystem;

        [SerializeField]
        [InlineIconButton("Refresh", "RecalculateAreaSize")]
        private DVector2 _AreaSize;

        [PropertySpace]
        [SerializeField]
        private DVector2 _Min;

        [SerializeField]
        private DVector2 _Max;

        public AreaRect(DVector2 min, DVector2 max, CoordinateSystemType coordinateSystem)
        {
            _Min = min;
            _Max = max;
            _CoordinateSystem = coordinateSystem;
        }

        public CoordinateSystemType CoordinateSystem => _CoordinateSystem;
        public DVector2 AreaSize => _AreaSize;

        public DVector2 Min => _Min;
        public DVector2 Max => _Max;
        public DVector2 Size => _Max - _Min;

        public double MinX => Min.X;
        public double MinY => Min.Y;
        public double MaxX => Max.X;
        public double MaxY => Max.Y;

        public string ToCommaSeparatedString()
        {
            return $"{MinX.ToString(CultureInfo.InvariantCulture)}," +
                   $"{MinY.ToString(CultureInfo.InvariantCulture)}," +
                   $"{MaxX.ToString(CultureInfo.InvariantCulture)}," +
                   $"{MaxY.ToString(CultureInfo.InvariantCulture)}";
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        public void RecalculateAreaSize(InspectorProperty property)
        {
            var xSize = GeoCoordinateUtility.Distance(new DVector2(MinX, MinY), new DVector2(MaxX, MinY), CoordinateSystem, Shape.Ellipsoid);
            var ySize = GeoCoordinateUtility.Distance(new DVector2(MinX, MinY), new DVector2(MinX, MaxY), CoordinateSystem, Shape.Ellipsoid);
            _AreaSize = new DVector2(xSize, ySize);
            property.MarkSerializationRootDirty();
        }
#endif
    }
}