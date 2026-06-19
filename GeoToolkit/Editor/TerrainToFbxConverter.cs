// Author: František Holubec
// Created: 09.06.2026

#if UNITY_EDITOR && FBX_EXPORTER
using System.IO;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;

namespace EDIVE.GeoToolkit.TerrainTools
{
    public class TerrainToFbxConverter : EditorWindow
    {
        [SerializeField]
        private Terrain _Terrain;

        [SerializeField]
        private int _Resolution = 256;

        [SerializeField]
        private bool _GenerateUV = true;

        [SerializeField]
        private bool _RecalculateNormals = true;

        [MenuItem("Tools/GeoToolkit/Terrain To FBX")]
        private static void Open()
        {
            var window = GetWindow<TerrainToFbxConverter>();
            window.titleContent = new GUIContent("Terrain To FBX");
            window.minSize = new Vector2(340, 200);
            window.Show();
        }

        private void OnEnable()
        {
            if (_Terrain == null)
                _Terrain = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Terrain>() : null;
            if (_Terrain == null)
                _Terrain = Terrain.activeTerrain;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Bakes a Terrain heightmap into a mesh and exports it as an FBX file.\nHigher resolution = more vertices = more detail.", MessageType.Info);

            _Terrain = (Terrain) EditorGUILayout.ObjectField("Terrain", _Terrain, typeof(Terrain), true);
            _Resolution = EditorGUILayout.IntSlider("Resolution", _Resolution, 2, 4096);
            _GenerateUV = EditorGUILayout.Toggle("Generate UVs", _GenerateUV);
            _RecalculateNormals = EditorGUILayout.Toggle("Recalculate Normals", _RecalculateNormals);

            var verts = (_Resolution + 1) * (_Resolution + 1);
            EditorGUILayout.LabelField("Vertices", verts.ToString("N0"));

            using (new EditorGUI.DisabledScope(_Terrain == null || _Terrain.terrainData == null))
            {
                if (GUILayout.Button("Convert & Export FBX", GUILayout.Height(30)))
                    ConvertAndExport();
            }
        }

        private void ConvertAndExport()
        {
            var path = EditorUtility.SaveFilePanel("Export Terrain FBX", Application.dataPath, $"{_Terrain.name}.fbx", "fbx");
            if (string.IsNullOrWhiteSpace(path))
                return;

            GameObject temp = null;
            try
            {
                var mesh = BuildMesh(_Terrain.terrainData);

                // ModelExporter exports a GameObject hierarchy, so wrap the mesh in a temporary object.
                temp = new GameObject(Path.GetFileNameWithoutExtension(path));
                temp.AddComponent<MeshFilter>().sharedMesh = mesh;
                temp.AddComponent<MeshRenderer>();

                EditorUtility.DisplayProgressBar("Terrain To FBX", "Exporting FBX...", 0.95f);
                var exportedPath = ModelExporter.ExportObject(path, temp);

                if (string.IsNullOrEmpty(exportedPath))
                {
                    Debug.LogError($"[TerrainToFbx] Export failed for '{path}'.");
                    return;
                }

                Debug.Log($"[TerrainToFbx] Exported '{Path.GetFileName(exportedPath)}' ({mesh.vertexCount:N0} verts, {mesh.triangles.Length / 3:N0} tris) to {exportedPath}");

                // If it landed inside the project, import and ping it.
                var relative = ToProjectRelativePath(exportedPath);
                if (relative != null)
                {
                    AssetDatabase.ImportAsset(relative, ImportAssetOptions.ForceUpdate);
                    var asset = AssetDatabase.LoadMainAssetAtPath(relative);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
            }
            finally
            {
                if (temp != null)
                    DestroyImmediate(temp);
                EditorUtility.ClearProgressBar();
            }
        }

        private Mesh BuildMesh(TerrainData data)
        {
            var size = data.size;
            var res = Mathf.Max(2, _Resolution);
            var step = res + 1;

            var mesh = new Mesh
            {
                name = $"{_Terrain.name}_Mesh",
                // Terrain meshes easily exceed 65k vertices.
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            var vertices = new Vector3[step * step];
            var uvs = _GenerateUV ? new Vector2[step * step] : null;
            var triangles = new int[res * res * 6];

            for (var z = 0; z <= res; z++)
            {
                var nz = (float) z / res;
                for (var x = 0; x <= res; x++)
                {
                    var nx = (float) x / res;
                    var index = z * step + x;

                    var height = data.GetInterpolatedHeight(nx, nz); // already in world-space units
                    vertices[index] = new Vector3(nx * size.x, height, nz * size.z);

                    if (uvs != null)
                        uvs[index] = new Vector2(nx, nz);
                }

                if (z % 16 == 0)
                    EditorUtility.DisplayProgressBar("Terrain To FBX", "Sampling heights...", nz * 0.8f);
            }

            var t = 0;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var bl = z * step + x;
                    var br = bl + 1;
                    var tl = bl + step;
                    var tr = tl + 1;

                    triangles[t++] = bl;
                    triangles[t++] = tl;
                    triangles[t++] = tr;

                    triangles[t++] = bl;
                    triangles[t++] = tr;
                    triangles[t++] = br;
                }
            }

            mesh.vertices = vertices;
            if (uvs != null) mesh.uv = uvs;
            mesh.triangles = triangles;
            if (_RecalculateNormals)
                mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            var full = Path.GetFullPath(absolutePath).Replace('\\', '/');
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            if (!projectRoot.EndsWith("/")) projectRoot += "/";
            return full.StartsWith(projectRoot) ? full.Substring(projectRoot.Length) : null;
        }
    }
}
#endif
