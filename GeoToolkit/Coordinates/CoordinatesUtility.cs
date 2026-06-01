// Author: František Holubec
// Created: 01.06.2026

using CoordinateSharp;
using Unity.Mathematics;

namespace EDIVE.GeoToolkit.Coordinates
{
    public static class CoordinatesUtility
    {
        public static MilitaryGridCoords ConvertToMilitaryGrid(this GeoCoords coords)
        {
            if (coords.CoordinateSystem != CoordinateSystemType.EPSG_4326)
                coords = coords.ConvertTo(CoordinateSystemType.EPSG_4326);

            var eagerLoad = new EagerLoad(EagerLoadType.UTM_MGRS);
            var coord = new Coordinate(coords.Position.y, coords.Position.x, eagerLoad);
            return new MilitaryGridCoords(coord.MGRS);
        }

        public static bool TryConvertToGeoCoords(this MilitaryGridCoords militaryGridCoords, CoordinateSystemType targetSystem, out GeoCoords result)
        {
            result = new GeoCoords(double2.zero, CoordinateSystemType.Unknown);
            if (!militaryGridCoords.TryGetReferenceSystem(out var mgrs))
                return false;

            var eagerLoad = new EagerLoad(false);
            var coord = MilitaryGridReferenceSystem.MGRStoLatLong(mgrs, eagerLoad);
            var position = new double2(coord.Longitude.DecimalDegree, coord.Latitude.DecimalDegree);
            if (targetSystem != CoordinateSystemType.EPSG_4326)
                position = GeoCoords.Convert(position, CoordinateSystemType.EPSG_4326, targetSystem);

            result = new GeoCoords(position, targetSystem);
            return true;
        }
    }
}
