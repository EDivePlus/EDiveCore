#if UNITY_SPLINES
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

namespace EDIVE.Procedural.SplineMesh
{
    [ExecuteInEditMode]
    public class SplineMeshBender : MonoBehaviour
    {
        private enum FillingMode
        {
            Once,
            Repeat,
            StretchOnce,
            StretchRepeat
        }

        [SerializeField]
        private SplineContainer _SplineContainer;
        
        [BoxGroup("Source Mesh")]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private ModifiedMesh _SourceMesh;

        [SerializeField]
        private MeshFilter _MeshFilter;

        [SerializeField]
        private MeshCollider _MeshCollider;

        [SerializeField]
        private Vector2 _DistanceInterval;

        [SerializeField]
        private FillingMode _Mode = FillingMode.StretchOnce;
        
        [ReadOnly]
        [SerializeField]
        private Mesh _ResultMesh;
        
        [HideInInspector]
        [SerializeField]
        private int _ResultMeshHash;

        [NonSerialized]
        private readonly Dictionary<float, CurveSample> _sampleCache = new();

        private void OnValidate()
        {
            Recalculate();
        }

        private void OnEnable()
        {
            Spline.Changed += OnSplineChanged;
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        private void OnSplineChanged(Spline targetSpline, int arg2, SplineModification arg3)
        {
            if (_SplineContainer == null || !_SplineContainer.Splines.Contains(targetSpline))
                return;
            Recalculate();
        }

        private void Recalculate()
        {
            if (_SplineContainer == null)
                return;
            
            var newHash = HashCode.Combine(_SplineContainer, _SourceMesh, _DistanceInterval, _Mode);
            if (_ResultMesh == null || newHash != _ResultMeshHash)
            {
                _ResultMesh = new Mesh();
                _ResultMesh.name = _SourceMesh.Mesh != null ? $"{_SourceMesh.Mesh.name} (modified)" : "Null";
                _ResultMeshHash = newHash;
            }

            _ResultMesh = _Mode switch
            {
                FillingMode.Once => FillOnce(),
                FillingMode.Repeat => FillRepeat(),
                FillingMode.StretchOnce => FillStretchOnce(),
                FillingMode.StretchRepeat => FillStretchRepeat(),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (_MeshFilter != null)
                _MeshFilter.sharedMesh = _ResultMesh;

            if (_MeshCollider != null)
                _MeshCollider.sharedMesh = _ResultMesh;
        }

        private Mesh FillOnce()
        {
            _sampleCache.Clear();
            var bent = new List<MeshVertex>(_SourceMesh.Vertices.Count);
            var splineLength = _SplineContainer.CalculateLength();

            foreach (var vert in _SourceMesh.Vertices)
            {
                var distance = vert.Position.z - _SourceMesh.MinZ;
                var sample = GetOrCacheSample(distance, () =>
                {
                    var distOnSpline = (_DistanceInterval.x + distance).PositiveModulo(splineLength);
                    var t = Mathf.InverseLerp(0f, splineLength, distOnSpline);
                    return SampleCurve(t);
                });
                bent.Add(sample.Bend(vert));
            }

            MeshUtility.Update(_ResultMesh,
                _SourceMesh.Mesh,
                _SourceMesh.Triangles,
                bent.Select(b => b.Position),
                bent.Select(b => b.Normal));
            return _ResultMesh;
        }

        private Mesh FillRepeat()
        {
            var splineLength = _SplineContainer.CalculateLength();
            var intervalLength = (_DistanceInterval.y == 0f ? splineLength : _DistanceInterval.y) - _DistanceInterval.x;
            var repetitionCount = Mathf.FloorToInt(intervalLength / _SourceMesh.Length);
            repetitionCount = Mathf.Max(1, repetitionCount);

            BuildRepeatedTrianglesAndUVs(repetitionCount,
                out var triangles,
                out var uv, out var uv2, out var uv3, out var uv4,
                out var uv5, out var uv6, out var uv7, out var uv8);

            var bent = new List<MeshVertex>(_SourceMesh.Vertices.Count * repetitionCount);

            var offset = 0f;
            for (var i = 0; i < repetitionCount; i++)
            {
                _sampleCache.Clear();

                foreach (var vert in _SourceMesh.Vertices)
                {
                    var distance = vert.Position.z - _SourceMesh.MinZ + offset;
                    var sample = GetOrCacheSample(distance, () =>
                    {
                        var distOnSpline = (_DistanceInterval.x + distance).PositiveModulo(splineLength);
                        var t = Mathf.InverseLerp(0f, splineLength, distOnSpline);
                        return SampleCurve(t);
                    });
                    bent.Add(sample.Bend(vert));
                }

                offset += _SourceMesh.Length;
            }

            MeshUtility.Update(_ResultMesh,
                _SourceMesh.Mesh,
                triangles,
                bent.Select(b => b.Position),
                bent.Select(b => b.Normal),
                uv, uv2, uv3, uv4, uv5, uv6, uv7, uv8);

            return _ResultMesh;
        }

        private Mesh FillStretchOnce()
        {
            var splineLength = _SplineContainer.CalculateLength();
            var intervalLength = (_DistanceInterval.y == 0f ? splineLength : _DistanceInterval.y) - _DistanceInterval.x;

            _sampleCache.Clear();
            var bent = new List<MeshVertex>(_SourceMesh.Vertices.Count);

            foreach (var vert in _SourceMesh.Vertices)
            {
                var localZ = Mathf.Abs(vert.Position.z - _SourceMesh.MinZ);
                var distanceRate = _SourceMesh.Length == 0f ? 0f : localZ / _SourceMesh.Length;

                var sample = GetOrCacheSample(distanceRate, () =>
                {
                    var distOnSpline = Mathf.Min(_DistanceInterval.x + intervalLength * distanceRate, splineLength);
                    var t = Mathf.InverseLerp(0f, splineLength, distOnSpline);
                    return SampleCurve(t);
                });

                bent.Add(sample.Bend(vert));
            }

            MeshUtility.Update(_ResultMesh,
                _SourceMesh.Mesh,
                _SourceMesh.Triangles,
                bent.Select(b => b.Position),
                bent.Select(b => b.Normal));
            return _ResultMesh;
        }

        private Mesh FillStretchRepeat()
        {
            var splineLength = _SplineContainer.CalculateLength();
            var intervalLength = (_DistanceInterval.y == 0f ? splineLength : _DistanceInterval.y) - _DistanceInterval.x;
            var repetitionCount = Mathf.FloorToInt(intervalLength / _SourceMesh.Length);
            repetitionCount = Mathf.Max(1, repetitionCount);

            BuildRepeatedTrianglesAndUVs(repetitionCount,
                out var triangles,
                out var uv, out var uv2, out var uv3, out var uv4,
                out var uv5, out var uv6, out var uv7, out var uv8);

            var bent = new List<MeshVertex>(_SourceMesh.Vertices.Count * repetitionCount);
            _sampleCache.Clear();

            for (var i = 0; i < repetitionCount; i++)
            {
                foreach (var vert in _SourceMesh.Vertices)
                {
                    var localZ = vert.Position.z - _SourceMesh.MinZ;
                    var distanceRate = _SourceMesh.Length == 0f ? 0f : localZ / _SourceMesh.Length;
                    var globalRate = (i + distanceRate) / repetitionCount;

                    var sample = GetOrCacheSample(globalRate, () =>
                    {
                        var endInterval = _DistanceInterval.y == 0f ? splineLength : _DistanceInterval.y;
                        var distOnSpline = Mathf.Lerp(_DistanceInterval.x, endInterval, globalRate);
                        var t = Mathf.InverseLerp(0f, splineLength, distOnSpline);
                        return SampleCurve(t);
                    });

                    bent.Add(sample.Bend(vert));
                }
            }

            MeshUtility.Update(_ResultMesh,
                _SourceMesh.Mesh,
                triangles,
                bent.Select(b => b.Position),
                bent.Select(b => b.Normal),
                uv, uv2, uv3, uv4, uv5, uv6, uv7, uv8);

            return _ResultMesh;
        }

        private void BuildRepeatedTrianglesAndUVs(
            int repetitionCount,
            out List<int> triangles,
            out List<Vector2> uv, out List<Vector2> uv2, out List<Vector2> uv3, out List<Vector2> uv4,
            out List<Vector2> uv5, out List<Vector2> uv6, out List<Vector2> uv7, out List<Vector2> uv8)
        {
            var baseVertCount = _SourceMesh.Vertices.Count;
            triangles = new List<int>(_SourceMesh.Triangles.Length * repetitionCount);
            uv = new List<Vector2>(_SourceMesh.Mesh.uv.Length * repetitionCount);
            uv2 = new List<Vector2>(_SourceMesh.Mesh.uv2.Length * repetitionCount);
            uv3 = new List<Vector2>(_SourceMesh.Mesh.uv3.Length * repetitionCount);
            uv4 = new List<Vector2>(_SourceMesh.Mesh.uv4.Length * repetitionCount);
            uv5 = new List<Vector2>(_SourceMesh.Mesh.uv5.Length * repetitionCount);
            uv6 = new List<Vector2>(_SourceMesh.Mesh.uv6.Length * repetitionCount);
            uv7 = new List<Vector2>(_SourceMesh.Mesh.uv7.Length * repetitionCount);
            uv8 = new List<Vector2>(_SourceMesh.Mesh.uv8.Length * repetitionCount);

            for (var i = 0; i < repetitionCount; i++)
            {
                var vertOffset = baseVertCount * i;
                foreach (var idx in _SourceMesh.Triangles)
                    triangles.Add(idx + vertOffset);

                uv.AddRange(_SourceMesh.Mesh.uv);
                uv2.AddRange(_SourceMesh.Mesh.uv2);
                uv3.AddRange(_SourceMesh.Mesh.uv3);
                uv4.AddRange(_SourceMesh.Mesh.uv4);
                uv5.AddRange(_SourceMesh.Mesh.uv5);
                uv6.AddRange(_SourceMesh.Mesh.uv6);
                uv7.AddRange(_SourceMesh.Mesh.uv7);
                uv8.AddRange(_SourceMesh.Mesh.uv8);
            }
        }

        private CurveSample GetOrCacheSample(float key, Func<CurveSample> sampler)
        {
            if (!_sampleCache.TryGetValue(key, out var sample))
            {
                sample = sampler();
                _sampleCache[key] = sample;
            }
            return sample;
        }

        private CurveSample SampleCurve(float t)
        {
            _SplineContainer.Evaluate(t, out var position, out var tangent, out var normal);
            position = transform.InverseTransformPoint(position);
            tangent = transform.InverseTransformVector(tangent);
            normal = transform.InverseTransformDirection(normal);
            return new CurveSample(position, tangent, normal);
        }
    }
}
#endif
