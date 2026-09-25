#if GEO_JSON
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

namespace EDIVE.GeoToolkit.Utils
{
    public static class GeoJsonExtensions
    {

        public static readonly Dictionary<Type, GeoJSONObjectType> CONTAINER_TO_TYPE = new()
        {
            {typeof(Point), GeoJSONObjectType.Point},
            {typeof(MultiPoint), GeoJSONObjectType.MultiPoint},
            {typeof(LineString), GeoJSONObjectType.LineString},
            {typeof(MultiLineString), GeoJSONObjectType.MultiLineString},
            {typeof(Polygon), GeoJSONObjectType.Polygon},
            {typeof(MultiPolygon), GeoJSONObjectType.MultiPolygon},
            {typeof(GeometryCollection), GeoJSONObjectType.GeometryCollection},
            {typeof(Feature), GeoJSONObjectType.Feature},
            {typeof(FeatureCollection), GeoJSONObjectType.FeatureCollection},
        };
        
        public static readonly Dictionary<GeoJSONObjectType, Type> TYPE_TO_CONTAINER = new()
        {
            {GeoJSONObjectType.Point, typeof(Point)},
            {GeoJSONObjectType.MultiPoint, typeof(MultiPoint)},
            {GeoJSONObjectType.LineString, typeof(LineString)},
            {GeoJSONObjectType.MultiLineString, typeof(MultiLineString)},
            {GeoJSONObjectType.Polygon, typeof(Polygon)},
            {GeoJSONObjectType.MultiPolygon, typeof(MultiPolygon)},
            {GeoJSONObjectType.GeometryCollection, typeof(GeometryCollection)},
            {GeoJSONObjectType.Feature, typeof(Feature)},
            {GeoJSONObjectType.FeatureCollection, typeof(FeatureCollection)},
        };

        
        public static double2 ToDouble2(this Point point)
        {
            return new double2(point.Coordinates.Longitude, point.Coordinates.Latitude);
        }

        public static double2 ToDouble2(this IPosition position)
        {
            return new double2(position.Longitude, position.Latitude);
        }

        public static float2 ToFloat2(this Point point)
        {
            return new float2((float) point.Coordinates.Longitude, (float) point.Coordinates.Latitude);
        }

        public static float2 ToFloat2(this IPosition position)
        {
            return new float2((float) position.Longitude, (float) position.Latitude);
        }
        
        public static bool TryGetProperty<T>(this Feature feature, string propertyID, out T result)
        {
            return feature.Properties.TryGetValue(propertyID, out result);
        }
        
        public static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T result)
        {
            if (dictionary.TryGetValue(key, out var oResult))
            {
                result = (T) Convert.ChangeType(oResult, typeof(T));
                return true;
            }
            
            result = default;
            return false;
        }
        
        // includePolygonRings false: skip rings inside polygons (e.g. for line queries)
        public static List<T> GetAllOfType<T>(this IGeoJSONObject geoJsonObject, Predicate<T> filter = null, bool includePolygonRings = true) where T : IGeoJSONObject
        {
            var containers = new List<T>();
            geoJsonObject.GetAllOfType(ref containers, includePolygonRings);
            if (filter != null)
            {
                containers = containers.Where(container => filter(container)).ToList();
            }
            return containers;
        }

        private static void GetAllOfType<T>(this IGeoJSONObject geoJsonObject, ref List<T> containers, bool includePolygonRings) where T : IGeoJSONObject
        {
            // Geometries add themselves in GetAllSubGeometries
            if (geoJsonObject is T typedGeoJsonObject && geoJsonObject is not IGeometryObject) containers.Add(typedGeoJsonObject);
            
            switch (geoJsonObject)
            {
                case FeatureCollection featureCollection:
                    foreach (var feature in featureCollection.Features) 
                        feature.GetAllOfType(ref containers, includePolygonRings);
                    break;
                case Feature feature:
                    if (feature.Geometry == null)
                        break;
                    var featureGeometries = feature.Geometry.GetAllSubGeometries<T>(includePolygonRings);
                    containers.AddRange(featureGeometries);
                    break;
                case IGeometryObject geometryCollection:
                    var subGeometries = geometryCollection.GetAllSubGeometries<T>(includePolygonRings);
                    containers.AddRange(subGeometries);
                    break;
            }
        } 
        
        public static List<T> GetAllSubGeometries<T>(this IGeometryObject geometryObject, bool includePolygonRings = true)
        {
            var geometries = new List<T>();
            geometryObject.GetAllSubGeometries(ref geometries, includePolygonRings);
            return geometries;
        }
        
        private static void GetAllSubGeometries<T>(this IGeometryObject geometryObject, ref List<T> geometries, bool includePolygonRings)
        {
            if (geometryObject is T typedGeometryObject) geometries.Add(typedGeometryObject);
            
            switch (geometryObject)
            {
                case GeometryCollection geometryCollection:
                    foreach (var geometry in geometryCollection.Geometries) 
                        geometry.GetAllSubGeometries(ref geometries, includePolygonRings);
                    break;
                case MultiLineString multiLineString:
                    foreach (var geometry in multiLineString.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries, includePolygonRings);
                    break;
                case MultiPoint multiPoint:
                    foreach (var geometry in multiPoint.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries, includePolygonRings);
                    break;
                case MultiPolygon multiPolygon:
                    foreach (var geometry in multiPolygon.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries, includePolygonRings);
                    break;
                case Polygon polygon:
                    if (!includePolygonRings)
                        break;
                    foreach (var geometry in polygon.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries, includePolygonRings);
                    break;
                case LineString _:
                case Point _:
                    break;
            }
        }
    }
}
#endif
