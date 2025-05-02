using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoGIS.Scripts.Utils
{
    public static class DVectorExtensions
    {
        public static DVector2 XY(this DVector3 v) { return new DVector2(v.X, v.Y); }
        public static DVector2 XZ(this DVector3 v) { return new DVector2(v.X, v.Z); }
        public static DVector2 YZ(this DVector3 v) { return new DVector2(v.Y, v.Z); }
        public static DVector2 YX(this DVector3 v) { return new DVector2(v.Y, v.X); }
        public static DVector2 ZX(this DVector3 v) { return new DVector2(v.Z, v.X); }
        public static DVector2 ZY(this DVector3 v) { return new DVector2(v.Z, v.Y); }

        public static DVector3 WithX(this DVector3 v, float x) => new(x, v.Y, v.Z);
        public static DVector3 WithY(this DVector3 v, float y) => new(v.X, y, v.Z);
        public static DVector3 WithZ(this DVector3 v, float z) => new(v.X, v.Y, z);

        public static DVector3 WithXY(this DVector3 v, float x, float y) => new(x, y, v.Z);
        public static DVector3 WithXZ(this DVector3 v, float x, float z) => new(x, v.Y, z);
        public static DVector3 WithYZ(this DVector3 v, float y, float z) => new(v.X, y, z);

        public static DVector2 WithX(this DVector2 v, float x) => new(x, v.Y);
        public static DVector2 WithY(this DVector2 v, float y) => new(v.X, y);
        public static DVector3 WithZ(this DVector2 v, float z) => new(v.X, v.Y, z);


        public static DVector3 ScaledToOne(this DVector3 v) => new(Math.Sign(v.X), Math.Sign(v.Y), Math.Sign(v.Z));

        public static DVector2 Random(this DVector2 v, float min = -1, float max = 1)
        {
            v.X = UnityEngine.Random.Range(min, max);
            v.Y = UnityEngine.Random.Range(min, max);
            return v;
        }

        public static DVector3 Random(this DVector3 v, float min = -1, float max = 1)
        {
            v.X = UnityEngine.Random.Range(min, max);
            v.Y = UnityEngine.Random.Range(min, max);
            v.Z = UnityEngine.Random.Range(min, max);
            return v;
        }

        public static DVector3 MultiplyElementWise(this DVector3 v, DVector3 u) => new(v.X * u.X, v.Y * u.Y, v.Z * u.Z);
        public static DVector2 MultiplyElementWise(this DVector2 v, DVector2 u) => new(v.X * u.X, v.Y * u.Y);
        public static DVector3 DivideElementWise(this DVector3 v, DVector3 u) => new(v.X / u.X, v.Y / u.Y, v.Z / u.Z);
        public static DVector2 DivideElementWise(this DVector2 v, DVector2 u) => new(v.X / u.X, v.Y / u.Y);

        public static DVector2[] SplitWeighted(this DVector2 vector, float[] weights)
        {
            var sum = weights.Sum();
            var diff = vector.Y - vector.X;

            var result = new DVector2[weights.Length];
            var currentMin = vector.X;
            for (var i = 0; i < weights.Length; i++)
            {
                var currentMax = i == weights.Length - 1 ? vector.Y : currentMin + diff / sum * weights[i];
                result[i] = new DVector2(currentMin, currentMax);
                currentMin = currentMax;
            }

            return result;
        }

        public static DVector2 GetCenter(this IEnumerable<DVector2> vectors)
        {
            var sum = new DVector2(0, 0);
            foreach (var vector in vectors)
            {
                sum += vector;
            }
            return sum / vectors.Count();
        }

        public static DVector2 Inverse(this DVector2 vector)
        {
            return new DVector2(1 / vector.X, 1 / vector.Y);
        }

        public static DVector3 Inverse(this DVector3 vector)
        {
            return new DVector3(1 / vector.X, 1 / vector.Y, 1 / vector.Z);
        }
    }
}