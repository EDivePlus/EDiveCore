// Author: František Holubec
// Created: 19.10.2026

using System.Collections.Generic;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    // Sliced position is affine in scale: pos(s) = pos + (s - 1) * offset.
    // CPU slicing applies it directly, the shader gets it baked (UV6 offset, UV7 slope).
    public static class SliceMath
    {
        public const int OFFSET_CHANNEL = 6;
        public const int SLOPE_CHANNEL = 7;

        private static readonly List<Vector3> TEMP_VERTICES = new();
        private static readonly List<Vector3> TEMP_NORMALS = new();
        private static readonly List<Vector4> TEMP_TANGENTS = new();
        private static readonly List<Vector3> TEMP_OFFSETS = new();
        private static readonly List<Vector3> TEMP_SLOPES = new();

        public static float AxisOffset(float value, float sliceMin, float sliceMax, float boundsMin, float boundsMax, ScaleAnchor anchor, out float slope)
        {
            var half = (boundsMax - boundsMin) * 0.5f;
            var shift = anchor switch
            {
                ScaleAnchor.Min => half,
                ScaleAnchor.Max => -half,
                _ => 0
            };

            slope = 0;
            if (value <= sliceMin)
                return shift - half;
            if (value >= sliceMax)
                return shift + half;

            var span = sliceMax - sliceMin;
            slope = 2 * half / span;
            return shift + half * (2 * (value - sliceMin) / span - 1);
        }

        public static Vector3 Offset(Vector3 point, ObjectScaleDetails details, out Vector3 slope)
        {
            var bounds = details.Bounds;
            var offset = new Vector3(
                AxisOffset(point.x, details.SliceMin.x, details.SliceMax.x, bounds.min.x, bounds.max.x, details.Anchor.X, out slope.x),
                AxisOffset(point.y, details.SliceMin.y, details.SliceMax.y, bounds.min.y, bounds.max.y, details.Anchor.Y, out slope.y),
                AxisOffset(point.z, details.SliceMin.z, details.SliceMax.z, bounds.min.z, bounds.max.z, details.Anchor.Z, out slope.z));
            return offset;
        }

        public static Bounds ScaledBounds(ObjectScaleDetails details)
        {
            var bounds = details.Bounds;
            var scale = details.Scale - Vector3.one;
            var min = bounds.min + Vector3.Scale(scale, Offset(bounds.min, details, out _));
            var max = bounds.max + Vector3.Scale(scale, Offset(bounds.max, details, out _));
            var result = new Bounds();
            result.SetMinMax(Vector3.Min(min, max), Vector3.Max(min, max));
            return result;
        }

        // Writes sliced vertices into target, meshToRoot maps mesh space into scaler space.
        public static void ScaleMesh(Mesh original, Mesh target, ObjectScaleDetails details, Matrix4x4 meshToRoot)
        {
            var rootToMesh = meshToRoot.inverse;
            var normalToRoot = rootToMesh.transpose;
            var normalToMesh = meshToRoot.transpose;
            var scale = details.Scale - Vector3.one;

            original.GetVertices(TEMP_VERTICES);
            original.GetNormals(TEMP_NORMALS);
            original.GetTangents(TEMP_TANGENTS);
            var hasNormals = TEMP_NORMALS.Count == TEMP_VERTICES.Count;
            var hasTangents = TEMP_TANGENTS.Count == TEMP_VERTICES.Count;

            for (var i = 0; i < TEMP_VERTICES.Count; i++)
            {
                var point = meshToRoot.MultiplyPoint3x4(TEMP_VERTICES[i]);
                var offset = Offset(point, details, out var slope);
                var stretch = Vector3.one + Vector3.Scale(scale, slope);
                TEMP_VERTICES[i] = rootToMesh.MultiplyPoint3x4(point + Vector3.Scale(scale, offset));

                if (hasNormals)
                {
                    var normal = normalToRoot.MultiplyVector(TEMP_NORMALS[i]).DivideElementWise(stretch);
                    TEMP_NORMALS[i] = normalToMesh.MultiplyVector(normal).normalized;
                }

                if (hasTangents)
                {
                    var tangent = TEMP_TANGENTS[i];
                    var direction = Vector3.Scale(meshToRoot.MultiplyVector(tangent), stretch);
                    direction = rootToMesh.MultiplyVector(direction).normalized;
                    TEMP_TANGENTS[i] = new Vector4(direction.x, direction.y, direction.z, tangent.w);
                }
            }

            target.SetVertices(TEMP_VERTICES);
            if (hasNormals)
                target.SetNormals(TEMP_NORMALS);
            if (hasTangents)
                target.SetTangents(TEMP_TANGENTS);
            target.RecalculateBounds();
        }

        // Bakes shader slicing data, the shader applies it with the _SliceScale material value.
        // Mesh sits at pivot in scaler space with meshScale and no rotation.
        public static void BakeShaderMesh(Mesh original, Mesh target, ObjectScaleDetails details, Vector3 pivot, Vector3 meshScale)
        {
            original.GetVertices(TEMP_VERTICES);

            TEMP_OFFSETS.Clear();
            TEMP_SLOPES.Clear();
            foreach (var vertex in TEMP_VERTICES)
            {
                var offset = Offset(pivot + Vector3.Scale(vertex, meshScale), details, out var slope);
                TEMP_OFFSETS.Add(offset.DivideElementWise(meshScale));
                TEMP_SLOPES.Add(slope);
            }

            target.SetUVs(OFFSET_CHANNEL, TEMP_OFFSETS);
            target.SetUVs(SLOPE_CHANNEL, TEMP_SLOPES);
        }
    }
}
