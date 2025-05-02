using System;
using System.Globalization;
using ProtoGIS.Scripts.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.Area
{
    [Serializable]
    [InlineProperty]
    public class AreaRect : IAreaRect
    {
        [LabelWidth(150)]
        [SerializeField]
        private CoordRefSystem _CoordRefSystem;

        [SerializeField]
        [LabelWidth(150)]
        private DVector2 _RealSize;

        [PropertySpace]
        [SerializeField]
        private DVector2 _Min;

        [SerializeField]
        private DVector2 _Max;

        public AreaRect(DVector2 min, DVector2 max, CoordRefSystem coordRefSystem)
        {
            _Min = min;
            _Max = max;
            _CoordRefSystem = coordRefSystem;
        }

        public CoordRefSystem CoordRefSystem => _CoordRefSystem;
        public DVector2 RealSize => _RealSize;

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
    }
}