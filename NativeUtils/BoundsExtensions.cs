using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class BoundsExtensions
    {
        // Index 0-7. Bit 0 picks max X, bit 1 max Y, bit 2 max Z.
        public static Vector3 GetCorner(this Bounds bounds, int index)
        {
            var min = bounds.min;
            var max = bounds.max;
            return new Vector3(
                (index & 1) == 0 ? min.x : max.x,
                (index & 2) == 0 ? min.y : max.y,
                (index & 4) == 0 ? min.z : max.z);
        }

        // Fills 8 corners through the matrix. Local bounds to world, for example.
        public static void GetCorners(this Bounds bounds, Matrix4x4 matrix, Vector3[] corners)
        {
            for (var i = 0; i < 8; i++)
                corners[i] = matrix.MultiplyPoint3x4(bounds.GetCorner(i));
        }
    }
}
