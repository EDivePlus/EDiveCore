using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class VectorExtensions
    {
        public static Vector2 XY(this Vector3 v) { return new Vector2(v.x, v.y); }
        public static Vector2 XZ(this Vector3 v) { return new Vector2(v.x, v.z); }
        public static Vector2 YZ(this Vector3 v) { return new Vector2(v.y, v.z); }
        public static Vector2 YX(this Vector3 v) { return new Vector2(v.y, v.x); }
        public static Vector2 ZX(this Vector3 v) { return new Vector2(v.z, v.x); }
        public static Vector2 ZY(this Vector3 v) { return new Vector2(v.z, v.y); }

        public static Vector3 WithX(this Vector3 v, float x) => new(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new(v.x, v.y, z);

        public static Vector3 WithXY(this Vector3 v, float x, float y) => new(x, y, v.z);
        public static Vector3 WithXZ(this Vector3 v, float x, float z) => new(x, v.y, z);
        public static Vector3 WithYZ(this Vector3 v, float y, float z) => new(v.x, y, z);

        public static Vector2 WithX(this Vector2 v, float x) => new(x, v.y);
        public static Vector2 WithY(this Vector2 v, float y) => new(v.x, y);
        public static Vector3 WithZ(this Vector2 v, float z) => new(v.x, v.y, z);

        public static Vector4 WithX(this Vector4 v, float x) => new(x, v.y, v.z, v.w);
        public static Vector4 WithY(this Vector4 v, float y) => new(v.x, y, v.z, v.w);
        public static Vector4 WithZ(this Vector4 v, float z) => new(v.x, v.y, z, v.w);
        public static Vector4 WithW(this Vector4 v, float w) => new(v.x, v.y, v.z, w);

        public static Vector3 ScaledToOne(this Vector3 v) => new(Mathf.Sign(v.x), Mathf.Sign(v.y), Mathf.Sign(v.z));

        public static Vector2 Random(this Vector2 v, float min = -1, float max = 1)
        {
            v.x = UnityEngine.Random.Range(min, max);
            v.y = UnityEngine.Random.Range(min, max);
            return v;
        }

        public static Vector3 Random(this Vector3 v, float min = -1, float max = 1)
        {
            v.x = UnityEngine.Random.Range(min, max);
            v.y = UnityEngine.Random.Range(min, max);
            v.z = UnityEngine.Random.Range(min, max);
            return v;
        }

        public static Vector3 MultiplyElementWise(this Vector3 v, Vector3 u) => new(v.x * u.x, v.y * u.y, v.z * u.z);
        public static Vector2 MultiplyElementWise(this Vector2 v, Vector2 u) => new(v.x * u.x, v.y * u.y);
        public static Vector3 DivideElementWise(this Vector3 v, Vector3 u) => new(v.x / u.x, v.y / u.y, v.z / u.z);
        public static Vector2 DivideElementWise(this Vector2 v, Vector2 u) => new(v.x / u.x, v.y / u.y);

        public static Vector2[] SplitWeighted(this Vector2 vector, float[] weights)
        {
            var sum = weights.Sum();
            var diff = vector.y - vector.x;

            var result = new Vector2[weights.Length];
            var currentMin = vector.x;
            for (var i = 0; i < weights.Length; i++)
            {
                var currentMax = i == weights.Length - 1 ? vector.y : currentMin + diff / sum * weights[i];
                result[i] = new Vector2(currentMin, currentMax);
                currentMin = currentMax;
            }

            return result;
        }

        public static Vector2 GetCenter(this IEnumerable<Vector2> vectors)
        {
            var sum = new Vector2(0, 0);
            foreach (var vector in vectors)
            {
                sum += vector;
            }
            return sum / vectors.Count();
        }

        public static Vector2 Inverse(this Vector2 vector)
        {
            return new Vector2(1 / vector.x, 1 / vector.y);
        }

        public static Vector3 Inverse(this Vector3 vector)
        {
            return new Vector3(1 / vector.x, 1 / vector.y, 1 / vector.z);
        }
    }
}
