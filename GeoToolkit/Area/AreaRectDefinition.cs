using ProtoGIS.Scripts.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.Area
{
    public class AreaRectDefinition : ScriptableObject, IAreaRect
    {
        [FormerlySerializedAs("area")]
        [SerializeField]
        [LabelWidth(150)]
        private AreaRect _Area;

        public CoordRefSystem CoordRefSystem => _Area.CoordRefSystem;
        public DVector2 RealSize => _Area.RealSize;

        public DVector2 Min => _Area.Min;
        public DVector2 Max => _Area.Max;
        public DVector2 Size => _Area.Size;

        public double MinX => _Area.MinX;
        public double MinY => _Area.MinY;
        public double MaxX => _Area.MaxX;
        public double MaxY => _Area.MaxY;

        public string ToCommaSeparatedString() => _Area.ToCommaSeparatedString();
        public static implicit operator AreaRect(AreaRectDefinition areaRectDefinition) => areaRectDefinition._Area;
    }
}
