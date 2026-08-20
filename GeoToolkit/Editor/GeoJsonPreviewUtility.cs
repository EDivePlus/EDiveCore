#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Coordinates;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit
{
    public static class GeoJsonPreviewUtility
    {
        private const int EDGE_SAMPLES = 16;

        public static void OpenPoint(GeoCoords coords)
        {
            var pos = coords.ConvertTo(CoordinateSystemType.EPSG_4326).Position;
            OpenFeatures($"{{\"type\":\"Feature\",\"properties\":{{}},\"geometry\":{{\"type\":\"Point\",\"coordinates\":{FormatPosition(pos)}}}}}");
        }

        public static void OpenArea(GeoAreaRect area) => OpenFeatures(ToFeature(area));

        public static void OpenAreas(IEnumerable<GeoAreaRect> areas) => OpenFeatures(areas.Select(ToFeature).ToArray());

        private static void OpenFeatures(params string[] features)
        {
            var json = features.Length == 1
                ? features[0]
                : $"{{\"type\":\"FeatureCollection\",\"features\":[{string.Join(",", features)}]}}";
            Application.OpenURL("https://geojson.io/#data=data:application/json," + Uri.EscapeDataString(json));
        }

        private static string ToFeature(GeoAreaRect area)
        {
            var ring = string.Join(",", SampleOutline(area).Select(FormatPosition));
            return $"{{\"type\":\"Feature\",\"properties\":{{\"name\":\"{area.CoordinateSystem.ToName()}\"}},\"geometry\":{{\"type\":\"Polygon\",\"coordinates\":[[{ring}]]}}}}";
        }

        // Edges are sampled in the native system so the reprojected outline shows its real deformation
        private static IEnumerable<double2> SampleOutline(GeoAreaRect area)
        {
            var corners = new[] { area.Min, new double2(area.Max.x, area.Min.y), area.Max, new double2(area.Min.x, area.Max.y), area.Min };
            for (var e = 0; e < 4; e++)
            {
                for (var i = 0; i < EDGE_SAMPLES; i++)
                {
                    var pos = math.lerp(corners[e], corners[e + 1], (double) i / EDGE_SAMPLES);
                    yield return GeoCoords.Convert(pos, area.CoordinateSystem, CoordinateSystemType.EPSG_4326);
                }
            }
            yield return GeoCoords.Convert(area.Min, area.CoordinateSystem, CoordinateSystemType.EPSG_4326);
        }

        private static string FormatPosition(double2 pos) => FormattableString.Invariant($"[{pos.x:F6},{pos.y:F6}]");
    }
}
#endif
