using System.Collections.Generic;
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

        // Grows the bounds by other, or takes other when not initialized yet
        public static void EncapsulateOrInit(this ref Bounds bounds, Bounds other, ref bool initialized)
        {
            if (initialized)
                bounds.Encapsulate(other);
            else
                bounds = other;
            initialized = true;
        }

        // False when there is no renderer to take bounds from
        public static bool TryGetBounds(this IReadOnlyList<Renderer> renderers, out Bounds bounds, bool enabledOnly = false)
        {
            bounds = default;
            var initialized = false;
            for (var i = 0; i < renderers.Count; i++)
            {
                if (!enabledOnly || renderers[i].enabled)
                    bounds.EncapsulateOrInit(renderers[i].bounds, ref initialized);
            }

            return initialized;
        }
    }
}
