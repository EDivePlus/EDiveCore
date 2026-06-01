// Author: František Holubec
// Created: 29.09.2021

using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.GeoToolkit.TerrainTools;
using EDIVE.GeoToolkit.Utils;
using EDIVE.NativeUtils;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    public class LinesParser : MonoBehaviour
    {
        [FilePath(Extensions = "json, geojson, txt")]
        [SerializeField]
        [PropertyOrder(-1)]
        private string geoJsonPath;

        [SerializeField]
        private MapController _Map;

        [SerializeField]
        private ProceduralLineConfig config;

        [SerializeField]
        private Material material;

        [PropertySpace]
        [SerializeField]
        private float heightSampleBias = 0;

        [Button]
        [PropertyOrder(-1)]
        public void LoadJson()
        {
            transform.DestroyChildrenImmediate();

            var jsonText = File.ReadAllText(PathUtility.GetAbsolutePath(geoJsonPath));
            var geoJsonObject = JsonConvert.DeserializeObject<FeatureCollection>(jsonText);
            if (geoJsonObject == null)
            {
                Debug.LogError("Could not parse JSON!");
                return;
            }

            var geoLines = geoJsonObject.GetAllOfType<LineString>();
            for (var i = 0; i < geoLines.Count; i++)
            {
                var geoLine = geoLines[i];
                var points = new List<Vector3>();
                var pointSum = Vector3.zero;
                foreach (var coordinate in geoLine.Coordinates)
                {
                    var coords = new GeoCoords(new double2(coordinate.Longitude, coordinate.Latitude), CoordinateSystemType.EPSG_4326);
                    var planePosition = _Map.ConvertToMapCoordinates(coords);
                    var newBorderPoint = _Map.TrySampleHeight(planePosition, out var hit, heightSampleBias) ? hit : planePosition;
                    points.Add(newBorderPoint);
                    pointSum += (Vector3) newBorderPoint;
                }

                var center = pointSum / geoLine.Coordinates.Count;
                center.y = _Map.transform.position.y;

                var newPoints = points.Select(point => point - center).ToList();

                var lineObject = new GameObject($"Line_{i}");
                lineObject.transform.SetParent(transform);
                lineObject.transform.position = center;
                var proceduralBorder = lineObject.AddComponent<ProceduralLine>();
                proceduralBorder.SetData(newPoints, config);
                proceduralBorder.MeshRenderer.sharedMaterial = material;
            }
        }
    }
}
