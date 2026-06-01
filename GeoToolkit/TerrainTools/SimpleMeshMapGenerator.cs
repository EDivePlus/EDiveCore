// Author: František Holubec
// Created: 02.05.2025

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using EDIVE.GeoToolkit.Utils;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace EDIVE.GeoToolkit.TerrainTools
{
    public class SimpleMeshMapGenerator : MonoBehaviour
    {
        [SerializeField]
        private Texture2D _HeightMap;

        [SerializeField]
        private float3 _Size;

        [SerializeField]
        private GameObject _MeshObject;

        [SerializeField]
        private Material _Material;

        [Button]
        private void GenerateMeshTerrains()
        {
            var heightMap = _HeightMap.LoadGrayScale();

            var heightMapFlat = heightMap.To1DArray();
            var nonZeroValues = heightMapFlat.Where(i => i != 0);
            var terrainMax = heightMapFlat.Max();
            var terrainMin = nonZeroValues.Min();

            heightMap.Remap(terrainMin, terrainMax, 0, 1);
            heightMap.Clamp(0, 1);

            if (heightMap.GetLength(0) == 0 || heightMap.GetLength(1) == 0)
            {
                Debug.LogError("Invalid Terrain");
                return;
            }

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var heightMapWidth = heightMap.GetLength(0);
            var heightMapHeight = heightMap.GetLength(1);

            var xValueSize = _Size.x / (heightMapWidth - 1);
            var yValueSize = _Size.z / (heightMapHeight - 1);
            for (var pX = 0; pX < heightMapWidth; pX++)
            {
                for (var pY = 0; pY < heightMapHeight; pY++)
                {
                    var value = heightMap[pX, pY];
                    var height = _Size.y * value;
                    vertices.Add(new Vector3(pX * xValueSize, height, pY * yValueSize));
                    uvs.Add(new Vector2(1f / (heightMapWidth-1) * pX, 1f / (heightMapHeight-1) * pY));
                }
            }

            for (var pX = 0; pX < heightMapWidth - 1; pX++)
            {
                for (var pY = 0; pY < heightMapHeight - 1; pY++)
                {
                    var a = pY + pX * heightMapHeight;
                    var b = pY + (pX + 1) * heightMapHeight;
                    var c = pY + 1 + (pX + 1) * heightMapHeight;
                    var d = pY + 1 + pX * heightMapHeight;

                    if ((vertices[a].y + vertices[b].y + vertices[c].y + vertices[d].y).Approximately(0))
                        continue;

                    triangles.AddRange(new[] {c, b, a});
                    triangles.AddRange(new[] {d, c, a});
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.Optimize();

            var terrain = new GameObject("Terrain");
            var terrainTr = terrain.transform;
            terrainTr.SetParent(transform);
            terrainTr.Reset();
            terrainTr.localPosition = Vector3.zero;

            var meshFilter = terrain.AddComponent<MeshFilter>();
            var meshCollider = terrain.AddComponent<MeshCollider>();
            var meshRenderer = terrain.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshRenderer.material = _Material;
        }

        [Button]
        public void SaveMesh()
        {
            var path = EditorUtility.SaveFilePanel("Save FBX", Application.dataPath, "Object.fbx", "fbx");
            if (!string.IsNullOrWhiteSpace(path))
            {
                ModelExporter.ExportObject(path, _MeshObject);
            }
        }
    }
}
#endif
