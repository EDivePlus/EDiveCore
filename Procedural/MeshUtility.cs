using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EDIVE.Procedural
{
    public class MeshUtility
    {
        /// <summary>
        /// Returns a mesh with reserved triangles to turn back the face culling.
        /// This is useful when a mesh needs to have a negative scale.
        /// </summary>
        public static int[] GetReversedTriangles(Mesh mesh)
        {
            var res = mesh.triangles.ToArray();
            var triangleCount = res.Length / 3;
            for (var i = 0; i < triangleCount; i++)
            {
                (res[i * 3], res[i * 3 + 1]) = (res[i * 3 + 1], res[i * 3]);
            }
            return res;
        }

        /// <summary>
        /// Returns a mesh similar to the given source plus given optional parameters.
        /// </summary>
        public static void Update(Mesh mesh,
            Mesh source,
            IEnumerable<int> triangles = null,
            IEnumerable<Vector3> vertices = null,
            IEnumerable<Vector3> normals = null,
            IEnumerable<Vector2> uv = null,
            IEnumerable<Vector2> uv2 = null,
            IEnumerable<Vector2> uv3 = null,
            IEnumerable<Vector2> uv4 = null,
            IEnumerable<Vector2> uv5 = null,
            IEnumerable<Vector2> uv6 = null,
            IEnumerable<Vector2> uv7 = null,
            IEnumerable<Vector2> uv8 = null)
        {
            mesh.hideFlags = source.hideFlags;
            mesh.indexFormat = source.indexFormat;
            mesh.triangles = Array.Empty<int>();
            mesh.vertices = vertices == null ? source.vertices : vertices.ToArray();
            mesh.normals = normals == null ? source.normals : normals.ToArray();
            mesh.uv = uv == null ? source.uv : uv.ToArray();
            mesh.uv2 = uv2 == null ? source.uv2 : uv2.ToArray();
            mesh.uv3 = uv3 == null ? source.uv3 : uv3.ToArray();
            mesh.uv4 = uv4 == null ? source.uv4 : uv4.ToArray();
            mesh.uv5 = uv5 == null ? source.uv5 : uv5.ToArray();
            mesh.uv6 = uv6 == null ? source.uv6 : uv6.ToArray();
            mesh.uv7 = uv7 == null ? source.uv7 : uv7.ToArray();
            mesh.uv8 = uv8 == null ? source.uv8 : uv8.ToArray();
            mesh.triangles = triangles == null ? source.triangles : triangles.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }
        
            private static readonly List<Vector3> TEMP_VERTICES = new();

        public static void SliceMesh(Mesh original, Mesh target, Vector3 size, Bounds bounds, Vector3 sliceMin, Vector3 sliceMax)
        {
            SliceMesh(original, target, size, bounds, sliceMin, sliceMax, Matrix4x4.identity);
        }

        public static void SliceMesh(Mesh original, Mesh target, Vector3 size, Bounds bounds, Vector3 sliceMin, Vector3 sliceMax, Matrix4x4 matrix)
        {
            sliceMin = Vector3.Max(bounds.min, sliceMin);
            sliceMax = Vector3.Min(bounds.max, sliceMax);
            
            TEMP_VERTICES.Clear();
            original.GetVertices(TEMP_VERTICES);
            for (var i = 0; i < TEMP_VERTICES.Count; i++)
            {
                TEMP_VERTICES[i] = SlicePoint(TEMP_VERTICES[i], size, bounds, sliceMin, sliceMax, matrix);
            }
            target.SetVertices(TEMP_VERTICES);
        }
        
        public static Vector3 SlicePoint(Vector3 point, Vector3 size, Bounds bounds, Vector3 sliceMin, Vector3 sliceMax, Matrix4x4 matrix)
        {
            point = matrix.MultiplyPoint3x4(point);
            point = SlicePoint(point, size, bounds, sliceMin, sliceMax);
            return matrix.inverse.MultiplyPoint3x4(point);
        }
        
        public static Vector3 SlicePoint(Vector3 point, Vector3 size, Bounds bounds, Vector3 sliceMin, Vector3 sliceMax)
        {
            point.x = SliceSingleAxis(point.x, size.x, bounds.min.x, bounds.max.x, sliceMin.x, sliceMax.x);
            point.y = SliceSingleAxis(point.y, size.y, bounds.min.y, bounds.max.y, sliceMin.y, sliceMax.y);
            point.z = SliceSingleAxis(point.z, size.z, bounds.min.z, bounds.max.z, sliceMin.z, sliceMax.z);
            return point;
        }

        public static float SliceSingleAxis(float value, float size, float boundsMin, float boundsMax, float sliceMin, float sliceMax)
        {
            if (value <= sliceMin)
                return boundsMin * size - (boundsMin - value);
            if (value >= sliceMax)
                return boundsMax * size - (boundsMax - value);
            
            return Mathf.Lerp(sliceMin * size, sliceMax * size, (value - sliceMin) / (sliceMax - sliceMin));
        }
        
        public static Matrix4x4 ChildToRootMatrix(Transform child, Transform root)
        {
            return root.worldToLocalMatrix * child.localToWorldMatrix;
        }
        
        public static Bounds CalculateBounds(Mesh mesh, Transform meshTransform, Transform root)
        {
            var matrix = ChildToRootMatrix(meshTransform, root);
            return CalculateBounds(mesh, matrix);
        }
        
        public static Bounds CalculateBounds(Mesh mesh, Matrix4x4 matrix)
        {
            if (mesh == null || mesh.vertices == null || mesh.vertices.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);
            
            var center = matrix.MultiplyPoint3x4(mesh.vertices[0]);
            var bounds = new Bounds(center, Vector3.zero);
            
            for (var i = 1; i < mesh.vertices.Length; i++)
            {
                var vert = matrix.MultiplyPoint3x4(mesh.vertices[i]);
                bounds.Encapsulate(vert);
            }

            return bounds;
        }
    }
}
