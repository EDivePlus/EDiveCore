using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

namespace EDIVE.GeoToolkit.TerrainTools
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralLine : MonoBehaviour
    {
        [SerializeField]
        [HideLabel]
        [OnValueChanged(nameof(RecalculateMesh), true)]
        private ProceduralLineConfig config = ProceduralLineConfig.Default;

        [SerializeField]
        [OnValueChanged(nameof(RecalculateMesh), true)]
        private List<Vector3> points;
        
        public ProceduralLineConfig Config
        {
            get => config;
            set
            {
                config = value; 
                RecalculateMesh();
            }
        }

        public List<Vector3> Points
        {
            get => points;
            set
            {
                points = value; 
                RecalculateMesh();
            }
        }
        
        private MeshFilter _meshFilter;
        public MeshFilter MeshFilter => _meshFilter ? _meshFilter : _meshFilter = GetComponent<MeshFilter>();

        private MeshRenderer _meshRenderer;
        public MeshRenderer MeshRenderer => _meshRenderer ? _meshRenderer : _meshRenderer = GetComponent<MeshRenderer>();


        public void SetData(List<Vector3> pointList)
        {
            points = pointList;
            RecalculateMesh();
        }
        
        public void SetData(List<Vector3> pointList, ProceduralLineConfig newConfig)
        {
            points = pointList;
            config = newConfig;
            RecalculateMesh();
        }

        public void RecalculateMesh()
        {
            if (points == null) return;
            
            var currentMesh = MeshFilter.sharedMesh;
            var mesh = currentMesh ? currentMesh : new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            var segmentUVWidth = 1 / (config.loop ? points.Count + 1 : points.Count);
            var heightMult = 1 / (config.width + 2 * config.height);
            
            for (var i = 0; i < points.Count; i++)
            {
                var prevP = points[(i - 1).PositiveModulo(points.Count)];
                var currentP = points[i];
                var nextP = points[(i + 1).PositiveModulo(points.Count)];

                Vector3 direction;
                if (!config.loop && points.Count == 1)
                { 
                    direction = Vector3.forward;
                }
                else if (!config.loop && i == 0)
                {
                    direction = (nextP - currentP).normalized;
                }
                else if (!config.loop && i == points.Count - 1)
                {
                    direction = (currentP - prevP).normalized;
                }
                else
                {
                    direction = (((currentP - prevP) + (nextP - currentP)) / 2).normalized;
                }

                var rVector3 = Vector3.Cross(direction, Vector3.up) * (config.width / 2);

                if (config.drawSides)
                {
                    var aPoint = currentP.WithY(0) + rVector3;
                    var bPoint = currentP + Vector3.up * config.height + rVector3;
                    var cPoint = currentP + Vector3.up * config.height - rVector3;
                    var dPoint = currentP.WithY(0) - rVector3;
                
                    vertices.AddRange(new[] {aPoint, bPoint, bPoint, cPoint, cPoint, dPoint});

                    var widthPos = segmentUVWidth * i;
                
                    var uvA = new Vector2(0, widthPos);
                    var uvB = new Vector2(config.height * heightMult, widthPos);
                    var uvC = new Vector2((config.height + config.width) * heightMult, widthPos);
                    var uvD = new Vector2(1, widthPos);
                
                    uvs.AddRange(new[] {uvA, uvB, uvB, uvC, uvC, uvD}); 
                }
                else
                {
                    var aPoint = currentP + Vector3.up * config.height + rVector3;
                    var bPoint = currentP + Vector3.up * config.height - rVector3;
                    vertices.AddRange(new[] {aPoint, bPoint});

                    var widthPos = segmentUVWidth * i;
                    var uvA = new Vector2(0, widthPos);
                    var uvB = new Vector2(1, widthPos);
                    
                    uvs.AddRange(new[] {uvA, uvB});
                }
                
            }

            if (config.loop && points.Count > 2)
            {
                if (config.drawSides)
                {
                    vertices.AddRange(new[] {vertices[0], vertices[1], vertices[2], vertices[3], vertices[4], vertices[5]});
                    var uvA = new Vector2(0, 1);
                    var uvB = new Vector2(config.height * heightMult, 1);
                    var uvC = new Vector2((config.height + config.width) * heightMult, 1);
                    var uvD = new Vector2(1, 1);
                    uvs.AddRange(new[] {uvA, uvB, uvB, uvC, uvC, uvD});
                }
                else
                {
                    vertices.AddRange(new[] {vertices[0], vertices[1]});
                    var uvA = new Vector2(0, 1);
                    var uvB = new Vector2(1, 1);
                    uvs.AddRange(new[] {uvA, uvB});
                }
            }
            
            for (var i = 0; i < points.Count; i++)
            {
                if (!config.loop && i == points.Count - 1) break;

                if (config.drawSides)
                {
                    var af = i * 6;
                    var bf = af + 1;
                    var bf2 = af + 2;
                    var cf = af + 3;
                    var cf2 = af + 4;
                    var df = af + 5;
                
                    var an = af + 6;
                    var bn = af + 7;
                    var bn2 = af + 8;
                    var cn = af + 9;
                    var cn2 = af + 10;
                    var dn = af + 11;

                    triangles.AddRange(new[] {af, an, bn});
                    triangles.AddRange(new[] {af, bn, bf});
                    triangles.AddRange(new[] {bf2, bn2, cn2});
                    triangles.AddRange(new[] {cf2, bf2, cn2});
                    triangles.AddRange(new[] {cf, cn, dn});
                    triangles.AddRange(new[] {df, cf, dn});
                }
                else
                {
                    var a1 = i * 2;
                    var b1 = a1 + 1;
                    var a2 = a1 + 2;
                    var b2 = a1 + 3;

                    triangles.AddRange(new[] {b1, a1, a2});
                    triangles.AddRange(new[] {b1, a2, b2});
                }
            }

            if (config.drawSides && !config.loop && points.Count > 0)
            {
                const int af = 0;
                const int bf = 1;
                const int cf = 3;
                const int df = 5;
                
                var al = (points.Count - 1) * 6;
                var bl = al + 1;
                var cl = al + 3;
                var dl = al + 5;
                
                triangles.AddRange(new[] {af, bf, cf});
                triangles.AddRange(new[] {af, cf, df});
                triangles.AddRange(new[] {al, cl, bl});
                triangles.AddRange(new[] {al, dl, cl});
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.Optimize();
            MeshFilter.sharedMesh = mesh;
        }
        
#if UNITY_EDITOR
        [CustomEditor(typeof(ProceduralLine))]
        [CanEditMultipleObjects]
        public class ProceduralLineEditor : OdinEditor
        {
            private void OnSceneGUI()
            {
                var smartValue = target as ProceduralLine;
                if (smartValue != null) smartValue.OnSceneGUI();
            }
        }

        [SerializeField]
        [HideInInspector]
        private bool editPointsMode;

        [PropertySpace]
        [Button]
        [GUIColor(0,0.8f,0)]
        [HideIf(nameof(editPointsMode))]
        private void EnableEditMode() => editPointsMode = true;

        [PropertySpace]
        [Button]
        [GUIColor(0.8f,0,0)]
        [ShowIf(nameof(editPointsMode))]
        private void DisableEditMode() => editPointsMode = false;

        private void OnSceneGUI()
        {
            if (points == null || !editPointsMode) return;
            for (var i = 0; i < points.Count; i++)
            {
                var prevWorldPosition = transform.TransformPoint(points[i]);
                Handles.Label(prevWorldPosition, $"P[{i}]");
                var newWorldPosition = Handles.PositionHandle(prevWorldPosition, Quaternion.identity);
                if (prevWorldPosition != newWorldPosition)
                {
                    points[i] = transform.InverseTransformPoint(newWorldPosition);
                    RecalculateMesh();
                }
            }
        }
#endif
    }
    
    [Serializable]
    public struct ProceduralLineConfig
    {
        public float height;
        public float width;
        public bool loop;
        public bool drawSides;

        public static ProceduralLineConfig Default = new ProceduralLineConfig()
        {
            drawSides = true
        };
    }
}
