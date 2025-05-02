using ProtoGIS.Scripts.Utils;

namespace EDIVE.GeoToolkit.Area
{
    public interface IAreaRect
    {
        DVector2 Min { get; }
        DVector2 Max { get; }
        DVector2 Size { get; }

        double MinX { get; }
        double MinY { get; }
        double MaxX { get; }
        double MaxY { get; }

        CoordRefSystem CoordRefSystem { get; }
        DVector2 RealSize { get; }

        string ToCommaSeparatedString();
    }
}
