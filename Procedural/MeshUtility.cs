using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
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

        public static void SliceScaleMesh(Mesh original, Mesh target, Vector3 referenceSize, Vector3 scale, Vector3 sliceMin, Vector3 sliceMax)
        {
            SliceScaleMesh(original, target, referenceSize, scale, sliceMin, sliceMax, Matrix4x4.identity);
        }
        
        public static void SliceScaleMesh(Mesh original, Mesh target, Vector3 referenceSize, Vector3 scale, Vector3 sliceMin, Vector3 sliceMax, Transform meshTransform, Transform root)
        {
            var matrix = ChildToRootMatrix(meshTransform, root);
            SliceScaleMesh(original, target, referenceSize, scale, sliceMin, sliceMax, matrix);
        }
        
        public static void SliceScaleMesh(Mesh original, Mesh target, Vector3 referenceSize, Vector3 scale, Vector3 sliceMin, Vector3 sliceMax, Matrix4x4 matrix)
        {
            var halfSize = referenceSize * 0.5f;
            var scaledHalf = halfSize.MultiplyElementWise(scale);
            var offset = scaledHalf - halfSize;
            
            TEMP_VERTICES.Clear();
            original.GetVertices(TEMP_VERTICES);
            for (var i = 0; i < TEMP_VERTICES.Count; i++)
            {
                TEMP_VERTICES[i] = SliceScalePoint(TEMP_VERTICES[i], sliceMin, sliceMax, offset, matrix);
            }
            target.SetVertices(TEMP_VERTICES);
        }

        private static Vector3 SliceScalePoint(Vector3 point, Vector3 sliceMin, Vector3 sliceMax, Vector3 offsets, Matrix4x4 matrix)
        {
            point = matrix.MultiplyPoint3x4(point);
            point = SliceScalePoint(point, sliceMin, sliceMax, offsets);
            return matrix.inverse.MultiplyPoint3x4(point);
        }

        private static Vector3 SliceScalePoint(Vector3 value, Vector3 sliceMin, Vector3 sliceMax, Vector3 offsets)
        {
            value.x = SliceScaleSingleAxis(value.x, sliceMin.x, sliceMax.x, offsets.x);
            value.y = SliceScaleSingleAxis(value.y, sliceMin.y, sliceMax.y, offsets.y);
            value.z = SliceScaleSingleAxis(value.z, sliceMin.z, sliceMax.z, offsets.z);
            return value;
        }

        private static float SliceScaleSingleAxis(float value, float sliceMin, float sliceMax, float offset)
        {
            if (value <= sliceMin)
                return value - offset;
            
            if (value >= sliceMax)
                return value + offset;
            
            return Mathf.Lerp(sliceMin - offset, sliceMax + offset, (value - sliceMin) / (sliceMax - sliceMin));
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
