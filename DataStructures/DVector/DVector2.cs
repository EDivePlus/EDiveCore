using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProtoGIS.Scripts.Utils
{
    /// <summary>
    /// Double vector for Unity.
    /// Holding structure of 2D vector with double values.
    /// </summary>
    [Serializable] 
    [InlineProperty]
    public struct DVector2
    {
        [FormerlySerializedAs("_x")]
        [SerializeField][FormerlySerializedAs("x")]
        [LabelWidth(8)]
        [HorizontalGroup]
        private double _X;
        
        [FormerlySerializedAs("_y")]
        [SerializeField][FormerlySerializedAs("y")]
        [LabelWidth(8)]
        [HorizontalGroup]
        private double _Y;
        
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

        public DVector2(double x, double y)
        {
            _X = x;
            _Y = y;
        }

        public DVector2(Vector3 v) : this(v.x, v.y)
        { }

        /// <summary>
        /// Downgrade the vector of double values to float values.
        /// IMPORTANT! The precision of the vector is lowered when doing this operation.
        /// </summary>
        /// <returns>Unity's Vector2.</returns>
        public Vector2 ToVector2() => new((float)X, (float)Y);

        public static readonly DVector2 Zero = new(0, 0);

        public double Size()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public static DVector2 operator +(DVector2 a, DVector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static DVector2 operator -(DVector2 a, DVector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static DVector2 operator *(int a, DVector2 b) => new(a * b.X, a * b.Y);
        public static DVector2 operator *(DVector2 a, int b) => new(a.X * b, a.Y * b);
        public static DVector2 operator *(double a, DVector2 b) => new(a * b.X, a * b.Y);
        public static DVector2 operator *(DVector2 a, double b) => new(a.X * b, a.Y * b);
        public static DVector2 operator /(DVector2 a, int b) => new(a.X / b, a.Y / b);
        public static DVector2 operator /(DVector2 a, double b) => new(a.X / b, a.Y / b);
        
        public static implicit operator Vector2(DVector2 vector) => vector.ToVector2();

        public static bool operator ==(DVector2 a, DVector2 b) => a.Equals(b);
        public static bool operator !=(DVector2 a, DVector2 b) => !a.Equals(b);

        public bool Equals(DVector2 other)
        {
            return _X.Equals(other._X) && _Y.Equals(other._Y);
        }

        public override bool Equals(object obj)
        {
            return obj is DVector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _X.GetHashCode();
                hashCode = (hashCode * 397) ^ _Y.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
