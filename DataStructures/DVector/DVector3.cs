using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProtoGIS.Scripts.Utils
{
    /// <summary>
    /// Double vector for Unity.
    /// Holding structure of 3D vector with double values.
    /// </summary>
    [Serializable] 
    [InlineProperty]
    public struct DVector3 : IEquatable<DVector3>
    {
        [FormerlySerializedAs("x")]
        [SerializeField][FormerlySerializedAs("_x")]
        [LabelWidth(8)]
        [HorizontalGroup]
        private double _X;
        
        [FormerlySerializedAs("y")]
        [SerializeField][FormerlySerializedAs("_y")]
        [LabelWidth(8)]
        [HorizontalGroup]
        private double _Y;
        
        [FormerlySerializedAs("z")]
        [SerializeField][FormerlySerializedAs("_z")]
        [LabelWidth(8)]
        [HorizontalGroup]
        private double _Z;

        public double X
        {
            get => _X;
            set => _X = value;
        }
        public double Y
        {
            get => _Y;
            set => _Y = value;
        }
        public double Z
        {
            get => _Z;
            set => _Z = value;
        }

        public static readonly DVector3 Zero = new(0, 0, 0);

        public DVector3(double x, double y, double z)
        {
            _X = x;
            _Y = y;
            _Z = z;
        }

        public DVector3(Vector3 v) : this(v.x, v.y, v.z)
        { }

        /// <summary>
        /// Downgrade the vector of double values to float values.
        /// IMPORTANT! The precision of the vector is lowered when doing this operation.
        /// </summary>
        /// <returns>Unity's Vector3.</returns>
        public Vector3 ToVector3() => new((float)X, (float)Y, (float)Z);

        /// <summary>
        /// Euclidean size of the vector.
        /// </summary>
        /// <returns>Euclidean size of the vector.</returns>
        public double Size()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static DVector3 operator +(DVector3 a, DVector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static DVector3 operator -(DVector3 a, DVector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static DVector3 operator *(int a, DVector3 b) => new(a * b.X, a * b.Y, a * b.Z);
        public static DVector3 operator *(DVector3 a, int b) => new(a.X * b, a.Y * b, a.Z * b);
        public static DVector3 operator *(double a, DVector3 b) => new(a * b.X, a * b.Y, a * b.Z);
        public static DVector3 operator *(DVector3 a, double b) => new(a.X * b, a.Y * b, a.Z * b);

        public static bool operator ==(DVector3 a, DVector3 b) => a.Equals(b);
        public static bool operator !=(DVector3 a, DVector3 b) => !a.Equals(b);

        public bool Equals(DVector3 other)
        {
            return _X.Equals(other._X) && _Y.Equals(other._Y) && _Z.Equals(other._Z);
        }

        public override bool Equals(object obj)
        {
            return obj is DVector3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_X, _Y, _Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
