using System;
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
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Polygon = TriangleNet.Geometry.Polygon;

#if UNITY_EDITOR
using GeoPolygon = GeoJSON.Net.Geometry.Polygon;
#endif

namespace EDIVE.GeoToolkit.Maps
{
    public class PolygonParser : MonoBehaviour
    {
        [Sirenix.OdinInspector.FilePath(Extensions = "json, geojson, txt")]
        [SerializeField]
        [PropertyOrder(-1)]
        private string geoJsonPath;

        [SerializeField]
        private MapController _Map;

        [SerializeField]
        private bool generateBorder = true;

        [SerializeField]
        private bool generateDecal = true;

        [SerializeField]
        [ShowIf(nameof(generateDecal))]
        private bool generateDecalCollider = false;

        [SerializeField]
        [Indent]
        [ShowIf(nameof(generateDecalCollider))]
        private bool linkHoverComponent = false;

        [SerializeField]
        private string groupingProperty = "Id";

        [Title("Decal", HorizontalLine = false)]
        [SerializeField]
        private Material decalMaterial;

        [SerializeField]
        private float minAreaSizePerPixel = 0.02f;

        [SerializeField]
        private int maxAreaTextureResolution = 4096;

        [Title("Border Line", HorizontalLine = false)]
        [SerializeField]
        private Material borderMaterial;

        [SerializeField]
        private float borderHeight;

        [SerializeField]
        private float borderWidth;

        [SerializeField]
        private float heightSampleBias = 0.01f;

        [PropertySpace]
        [SerializeField]
        [FolderPath]
        [InfoBox("Non-existing folder will be created!", InfoMessageType.None)]
        private string polygonAssetsFolder;

        private string TexturesFolderPath => $"{polygonAssetsFolder}/Textures";

        private static readonly int MASK_TEX = Shader.PropertyToID("_BaseMap");

        private static GeoCoords ToGeoCoords(IPosition position)
        {
            return new GeoCoords(new double2(position.Longitude, position.Latitude), CoordinateSystemType.EPSG_4326);
        }

        /// <summary>Samples the map height at a world-space XZ position, falling back to the map plane height.</summary>
        private float3 SampleHeight(float2 worldXZ, float bias)
        {
            var planePoint = new float3(worldXZ.x, _Map.MapTransformData.Origin.y, worldXZ.y);
            return _Map.TrySampleHeight(planePoint, out var hit, bias) ? hit : planePoint;
        }

#if UNITY_EDITOR
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

            PathUtility.EnsureAssetsPathExists(TexturesFolderPath);

            var polygonGroups = new Dictionary<string, List<PolygonController>>();
            var geoFeatures = geoJsonObject.GetAllOfType<Feature>(feature => feature.Geometry is GeoPolygon);
            for (var p = 0; p < geoFeatures.Count; p++)
            {
                var geoFeature = geoFeatures[p];
                var borderPointLists = new List<List<Vector3>>();
                var geoPolygon = geoFeature.Geometry as GeoPolygon;
                var polygon = new Polygon();

                var minPoint = Vector2.positiveInfinity;
                var maxPoint = Vector2.negativeInfinity;
                foreach (var lineString in geoPolygon.Coordinates)
                {
                    foreach (var coordinate in lineString.Coordinates)
                    {
                        var position = _Map.ConvertToMapCoordinates(ToGeoCoords(coordinate)).xz;
                        minPoint = Vector2.Min(minPoint, position);
                        maxPoint = Vector2.Max(maxPoint, position);
                    }
                }

                var originPosition2D = (minPoint + maxPoint) / 2;
                var terrainOrigin = _Map.MapTransformData.Origin;
                var originPosition = new Vector3(originPosition2D.x, terrainOrigin.y, originPosition2D.y);

                for (var i = 0; i < geoPolygon.Coordinates.Count; i++)
                {
                    var borderPoints = new List<Vector3>();
                    var lineString = geoPolygon.Coordinates[i];
                    var worldPoints = new List<Vertex>();

                    var positions = new List<Vector2>();
                    foreach (var coordinate in lineString.Coordinates)
                    {
                        var position = _Map.ConvertToMapCoordinates(ToGeoCoords(coordinate)).xz;
                        positions.Add(position);
                    }

                    foreach (var position in positions)
                    {
                        worldPoints.Add(new Vertex(position.x - originPosition.x, position.y - originPosition.z, i + 1));
                        var newBorderPoint = SampleHeight(position, heightSampleBias) - (float3) originPosition;
                        borderPoints.Add(newBorderPoint);
                        minPoint = Vector3.Min(minPoint, (Vector3) newBorderPoint);
                        maxPoint = Vector3.Max(maxPoint, (Vector3) newBorderPoint);
                    }

                    var contour = new Contour(worldPoints);
                    polygon.Add(contour, i > 0);
                    borderPointLists.Add(borderPoints);
                }

                var rootPolygonObject = new GameObject($"Polygon_{p}");
                rootPolygonObject.transform.SetParent(transform);
                rootPolygonObject.transform.position = originPosition;
                var polygonController = rootPolygonObject.AddComponent<PolygonController>();
                polygonController.Properties = geoFeature.Properties.ToDictionary(o => o.Key, o => o.Value.ToString());
                if (polygonController.Properties.TryGetValue(groupingProperty, out var result))
                {
                    if (polygonGroups.TryGetValue(result, out var controllers))
                    {
                        controllers.Add(polygonController);
                    }
                    else
                    {
                        polygonGroups[result] = new List<PolygonController> {polygonController};
                    }
                }

                if (generateDecal)
                {
                    // Generate Mesh
                    var mesh = GenerateMesh(polygon, originPosition);
                    var min = mesh.bounds.min;
                    var max = mesh.bounds.max;
                    var terrainHeight = _Map.MapTransformData.Size.y;

                    // Create Projector Object
                    var projectorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    projectorObject.gameObject.name = "Projector";
                    GameObjectExtensions.DestroyComponentImmediate<Collider>(projectorObject);
                    //projectorObject.AddComponent<DecalVisualizer>();
                    projectorObject.transform.SetParent(rootPolygonObject.transform);
                    var projectorRenderer = projectorObject.GetComponent<MeshRenderer>();
                    projectorRenderer.sharedMaterial = new Material(decalMaterial);
                    projectorObject.transform.position = originPosition + Vector3.up * terrainHeight / 2;
                    projectorObject.transform.localScale = new Vector3(max.x - min.x, terrainHeight, max.z - min.z);

                    // Setup Camera
                    var cameraObject = new GameObject("Camera");
                    cameraObject.transform.SetParent(projectorObject.transform);
                    cameraObject.transform.localPosition = Vector3.zero;
                    cameraObject.transform.localScale = Vector3.one;
                    cameraObject.transform.localEulerAngles = new Vector3(90, 0, 0);
                    var cam = cameraObject.AddComponent<Camera>();
                    cam.nearClipPlane = -0.1f;
                    cam.farClipPlane = 0.1f;
                    cam.cullingMask = 1 << 3;
                    cam.orthographic = true;
                    cam.clearFlags = CameraClearFlags.Color;
                    cam.backgroundColor = Color.clear;
                    cam.orthographicSize = mesh.bounds.extents.z;
                    cam.aspect = mesh.bounds.size.x / mesh.bounds.size.z;

                    // Instantiate Mesh
                    var meshObject = new GameObject("Mesh");
                    var meshRenderer = meshObject.AddComponent<MeshRenderer>();
                    var areaMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    meshRenderer.sharedMaterial = areaMaterial;
                    var meshFilter = meshObject.AddComponent<MeshFilter>();
                    meshFilter.mesh = mesh;
                    meshObject.transform.SetParent(projectorObject.transform);
                    meshObject.transform.position = originPosition;

                    // Render Mesh
                    var xSize = Math.Min((mesh.bounds.size.x / minAreaSizePerPixel).CeilToPowerOfTwo(), maxAreaTextureResolution);
                    var ySize = Math.Min((mesh.bounds.size.z / minAreaSizePerPixel).CeilToPowerOfTwo(), maxAreaTextureResolution);
                    var renderTexture = new RenderTexture(xSize, ySize, 0);
                    renderTexture.Create();
                    RenderTexture.active = renderTexture;
                    cam.targetTexture = renderTexture;

                    meshObject.layer = 3;
                    cam.Render();

                    var texture = new Texture2D(xSize, ySize);
                    texture.ReadPixels(new Rect(0, 0, xSize, ySize), 0, 0);
                    texture.Apply(true);

                    var texturePath = $"{TexturesFolderPath}/PolygonTexture_{p}.png";
                    File.WriteAllBytes(PathUtility.GetAbsolutePath(texturePath), texture.EncodeToPNG());
                    AssetDatabase.ImportAsset(texturePath);
                    var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    if (textureImporter != null)
                    {
                        textureImporter.isReadable = true;
                        textureImporter.wrapMode = TextureWrapMode.Mirror;
                        textureImporter.alphaIsTransparency = true;
                    }

                    // Clean Up
                    DestroyImmediate(cameraObject);
                    DestroyImmediate(meshObject);
                    DestroyImmediate(texture);
                    renderTexture.Release();

                    AssetDatabase.ImportAsset(texturePath);
                    AssetDatabase.Refresh();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    projectorRenderer.sharedMaterial.SetTexture(MASK_TEX, texture);
                    RenderTexture.active = null;

                    // Collider
                    if (generateDecalCollider)
                    {
                        var colliderObject = new GameObject("Collider");
                        colliderObject.transform.SetParent(rootPolygonObject.transform);
                        colliderObject.transform.position = originPosition;
                        var meshCollider = colliderObject.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = mesh;

                        if (linkHoverComponent)
                        {
                            /*
                            var hoverComponent = colliderObject.AddComponent<EnableOnHover>();
                            polygonController.HoverComponent = hoverComponent;
                            var interactableComponent = colliderObject.GetOrAddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                            interactableComponent.colliders.Add(meshCollider);
                            */
                        }
                    }
                }

                if (generateBorder)
                {
                    // Generate Border Meshes
                    for (var i = 0; i < borderPointLists.Count; i++)
                    {
                        var borderObject = new GameObject(i == 0 ? "OuterBorder" : $"InnerBorder_{i - 1}");
                        borderObject.transform.position = originPosition;
                        borderObject.transform.SetParent(rootPolygonObject.transform);
                        var proceduralBorder = borderObject.AddComponent<ProceduralLine>();
                        proceduralBorder.SetData(borderPointLists[i], new ProceduralLineConfig
                        {
                            height = borderHeight,
                            width = borderWidth,
                            loop = true
                        });
                        proceduralBorder.MeshRenderer.sharedMaterial = borderMaterial;
                    }
                }
            }

            foreach (var group in polygonGroups)
            {
                /*
                var allProjectors = group.Value.Select(g => g.GetComponentInChildren<DecalVisualizer>().gameObject).ToList();
                foreach (var polygon in group.Value)
                {
                    foreach (var projector in allProjectors)
                    {
                        polygon.HoverComponent.AddTarget(projector);
                    }
                }
                */
            }
        }
#endif

        private Mesh GenerateMesh(IPolygon polygon, Vector3 originPosition)
        {
            var mesh = new Mesh();

            var constraints = new ConstraintOptions {ConformingDelaunay = true};
            if (!(polygon.Triangulate(constraints) is TriangleNet.Mesh triangleMesh)) return mesh;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            foreach (var triangle in triangleMesh.Triangles)
            {
                var p0 = new Vector2((float) triangle.GetVertex(2).X, (float) triangle.GetVertex(2).Y);
                var p1 = new Vector2((float) triangle.GetVertex(1).X, (float) triangle.GetVertex(1).Y);
                var p2 = new Vector2((float) triangle.GetVertex(0).X, (float) triangle.GetVertex(0).Y);

                foreach (var point in new[] {p0, p1, p2})
                {
                    var vert = SampleHeight(point + originPosition.XZ(), heightSampleBias) - (float3) originPosition;
                    // var vert = new Vector3(point.x, 0, point.y);
                    triangles.Add(vertices.Count);
                    vertices.Add(vert);
                    uvs.Add(new Vector2(point.x / 100 + 0.5f, point.y / 100 + 0.5f));
                }
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
