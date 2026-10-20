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
            if (mesh == null)
                return new Bounds(Vector3.zero, Vector3.zero);
            var vertices = mesh.vertices;
            if (vertices.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);
            var center = matrix.MultiplyPoint3x4(vertices[0]);
            var bounds = new Bounds(center, Vector3.zero);
            for (var i = 1; i < vertices.Length; i++)
            {
                var vert = matrix.MultiplyPoint3x4(vertices[i]);
                bounds.Encapsulate(vert);
            }

            return bounds;
        }
    }
}
