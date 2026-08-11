// Author: František Holubec
// Created: 11.08.2026

using Unity.Mathematics;

namespace EDIVE.GeoToolkit.Area
{
    public enum GeoAreaAnchor
    {
        Center,
        SouthWest,
        SouthEast,
        NorthWest,
        NorthEast,
    }

    public static class GeoAreaAnchorUtility
    {
        public static double2 GetPivot(this GeoAreaAnchor anchor)
        {
            return anchor switch
            {
                GeoAreaAnchor.SouthWest => new double2(0, 0),
                GeoAreaAnchor.SouthEast => new double2(1, 0),
                GeoAreaAnchor.NorthWest => new double2(0, 1),
                GeoAreaAnchor.NorthEast => new double2(1, 1),
                _ => new double2(0.5, 0.5)
            };
        }
    }
}
