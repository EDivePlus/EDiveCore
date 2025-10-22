using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Utils.MeshScaling
{
    public static class MeshSlicingUtility
    {
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
