using CoordinateSharp;
using ProjNet.CoordinateSystems.Transformations;
using ProtoGIS.Scripts.Utils;

namespace EDIVE.GeoToolkit.Coordinates
{
    public static class GeoCoordinateUtility
    {
        public static DVector2 Convert(DVector2 pos, string sourceSystem, string targetSystem)
        {
            return Convert(pos, CoordinateSystemTypeUtility.Parse(sourceSystem), CoordinateSystemTypeUtility.Parse(targetSystem));
        }

        public static DVector2 Convert(DVector2 pos, CoordinateSystemType sourceSystemType, CoordinateSystemType targetSystemType)
        {
            var ctFact = new CoordinateTransformationFactory();
            var transformation = ctFact.CreateFromCoordinateSystems(sourceSystemType.GetCoordinateSystem(), targetSystemType.GetCoordinateSystem());
            var result = transformation.MathTransform.Transform(new []{ pos.X, pos.Y });
            return new DVector2(result[0], result[1]);
        }

        public static string CoordsToMGRS(DVector2 pos, CoordinateSystemType sourceSystem)
        {
            if (sourceSystem != CoordinateSystemType.EPSG_4326)
                pos = Convert(pos, sourceSystem, CoordinateSystemType.EPSG_4326);

            var eagerLoad = new EagerLoad(EagerLoadType.UTM_MGRS);
            var coord = new Coordinate(pos.Y, pos.X, eagerLoad);
            var mgrs = coord.MGRS;
            return mgrs.ToString();
        }

        public static bool TryMGRSToCoords(string mgrsPos, CoordinateSystemType targetSystem, out DVector2 result)
        {
            result = DVector2.Zero;
            if (!MilitaryGridReferenceSystem.TryParse(mgrsPos, out var mgrs))
                return false;

            var eagerLoad = new EagerLoad(false);
            var coords = MilitaryGridReferenceSystem.MGRStoLatLong(mgrs, eagerLoad);
            result = new DVector2(coords.Longitude.DecimalDegree, coords.Latitude.DecimalDegree);
            if (targetSystem != CoordinateSystemType.EPSG_4326)
                result = Convert(result, CoordinateSystemType.EPSG_4326, targetSystem);
            return true;

        }
        public static DVector2 MGRSToCoords(string mgrsPos, CoordinateSystemType targetSystem)
        {
            return TryMGRSToCoords(mgrsPos, targetSystem, out var result) ? result : DVector2.Zero;
        }

        public static double Distance(DVector2 posA, DVector2 posB, CoordinateSystemType targetSystem, Shape shape = Shape.Sphere)
        {
            if (targetSystem != CoordinateSystemType.EPSG_4326)
            {
                posA = Convert(posA, targetSystem, CoordinateSystemType.EPSG_4326);
                posB = Convert(posB, targetSystem, CoordinateSystemType.EPSG_4326);
            }
            var eagerLoad = new EagerLoad(false);
            var coordA = new Coordinate(posA.X, posA.Y, eagerLoad);
            var coordB = new Coordinate(posB.X, posB.Y, eagerLoad);
            var dist = new Distance(coordA, coordB, shape);
            return dist.Meters;
        }
    }
}
