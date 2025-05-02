using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ProtoGIS.Scripts.Utils;
using UnityEngine;

namespace EDIVE.GeoToolkit
{
    public static class GeoJsonExtensions
    {
        public static readonly Dictionary<Type, GeoJSONObjectType> CONTAINER_TO_TYPE = new Dictionary<Type, GeoJSONObjectType>
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
        
        public static readonly Dictionary<GeoJSONObjectType, Type> TYPE_TO_CONTAINER = new Dictionary<GeoJSONObjectType, Type>
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

        
        public static DVector2 ToDVector2(this Point point)
        {
            return new DVector2(point.Coordinates.Longitude, point.Coordinates.Latitude);
        }
        
        public static DVector2 ToDVector2(this IPosition position)
        {
            return new DVector2(position.Longitude, position.Latitude);
        }
        
        public static Vector2 ToVector2(this Point point)
        {
            return point.ToDVector2().ToVector2();
        }
        
        public static Vector2 ToVector2(this IPosition position)
        {
            return position.ToDVector2().ToVector2();
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
        
        public static List<T> GetAllOfType<T>(this IGeoJSONObject geoJsonObject, Predicate<T> filter = null) where T : IGeoJSONObject
        {
            var containers = new List<T>();
            geoJsonObject.GetAllOfType(ref containers);
            if (filter != null)
            {
                containers = containers.Where(container => filter(container)).ToList();
            }
            return containers;
        }

        private static void GetAllOfType<T>(this IGeoJSONObject geoJsonObject, ref List<T> containers) where T : IGeoJSONObject
        {
            if (geoJsonObject is T typedGeoJsonObject) containers.Add(typedGeoJsonObject);
            
            switch (geoJsonObject)
            {
                case FeatureCollection featureCollection:
                    foreach (var feature in featureCollection.Features) 
                        feature.GetAllOfType<T>(ref containers);
                    break;
                case Feature feature:
                    var featureGeometries = feature.Geometry.GetAllSubGeometries<T>();
                    containers.AddRange(featureGeometries);
                    break;
                case IGeometryObject geometryCollection:
                    var subGeometries = geometryCollection.GetAllSubGeometries<T>();
                    containers.AddRange(subGeometries);
                    break;
            }
        } 
        
        public static List<T> GetAllSubGeometries<T>(this IGeometryObject geometryObject)
        {
            var geometries = new List<T>();
            geometryObject.GetAllSubGeometries(ref geometries);
            return geometries;
        }
        
        private static void GetAllSubGeometries<T>(this IGeometryObject geometryObject, ref List<T> geometries)
        {
            if (geometryObject is T typedGeometryObject) geometries.Add(typedGeometryObject);
            
            switch (geometryObject)
            {
                case GeometryCollection geometryCollection:
                    foreach (var geometry in geometryCollection.Geometries) 
                        geometry.GetAllSubGeometries(ref geometries);
                    break;
                case MultiLineString multiLineString:
                    foreach (var geometry in multiLineString.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries);
                    break;
                case MultiPoint multiPoint:
                    foreach (var geometry in multiPoint.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries);
                    break;
                case MultiPolygon multiPolygon:
                    foreach (var geometry in multiPolygon.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries);
                    break;
                case Polygon polygon:
                    foreach (var geometry in polygon.Coordinates) 
                        geometry.GetAllSubGeometries(ref geometries);
                    break;
                case LineString _:
                case Point _:
                    break;
            }
        }
    }
}
