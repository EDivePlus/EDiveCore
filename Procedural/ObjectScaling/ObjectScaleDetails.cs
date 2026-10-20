// Author: František Holubec
// Created: 22.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    public enum ScaleAnchor
    {
        Center,
        Min,
        Max
    }

    [Serializable]
    public struct ScaleAnchors : IEquatable<ScaleAnchors>
    {
        [SerializeField]
        [HorizontalGroup]
        [LabelText("X")]
        [LabelWidth(14)]
        private ScaleAnchor _X;

        [SerializeField]
        [HorizontalGroup]
        [LabelText("Y")]
        [LabelWidth(14)]
        private ScaleAnchor _Y;

        [SerializeField]
        [HorizontalGroup]
        [LabelText("Z")]
        [LabelWidth(14)]
        private ScaleAnchor _Z;

        public ScaleAnchors(ScaleAnchor x, ScaleAnchor y, ScaleAnchor z)
        {
            _X = x;
            _Y = y;
            _Z = z;
        }

        public ScaleAnchor X => _X;
        public ScaleAnchor Y => _Y;
        public ScaleAnchor Z => _Z;

        public ScaleAnchor this[int axis] => axis switch
        {
            0 => _X,
            1 => _Y,
            _ => _Z
        };

        public bool Equals(ScaleAnchors other) => _X == other._X && _Y == other._Y && _Z == other._Z;
        public override bool Equals(object obj) => obj is ScaleAnchors other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int) _X, (int) _Y, (int) _Z);
    }

    [Serializable]
    public class ObjectScaleDetails
    {
        [SerializeField]
        [Tooltip("Size in scaler local space.")]
        private Vector3 _Size = Vector3.one;

        [SerializeField]
        [Tooltip("Side that stays in place when the size changes.")]
        private ScaleAnchors _Anchor;

        [SerializeField]
        private Vector3 _SliceMin = Vector3.zero;

        [SerializeField]
        private Vector3 _SliceMax = Vector3.zero;

        [Header("Bounds")]
        [HideLabel]
        [InlineProperty]
        [ReadOnly]
        [SerializeField]
        private Bounds _Bounds;

        public ref Vector3 Size => ref _Size;
        public ref ScaleAnchors Anchor => ref _Anchor;
        public ref Vector3 SliceMin => ref _SliceMin;
        public ref Vector3 SliceMax => ref _SliceMax;
        public ref Bounds Bounds => ref _Bounds;

        public Vector3 Scale
        {
            get
            {
                var reference = _Bounds.size;
                return new Vector3(AxisScale(_Size.x, reference.x), AxisScale(_Size.y, reference.y), AxisScale(_Size.z, reference.z));
            }
        }

        private static float AxisScale(float size, float reference) => reference > 1e-6f ? size / reference : 1;

        protected bool Equals(ObjectScaleDetails other)
        {
            return _Size.Equals(other._Size) && _Anchor.Equals(other._Anchor) && _SliceMin.Equals(other._SliceMin) && _SliceMax.Equals(other._SliceMax) && _Bounds.Equals(other._Bounds);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ObjectScaleDetails) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_Size, _Anchor, _SliceMin, _SliceMax, _Bounds);
        }

        // Shape without size, meshes shared by any size use it
        public Hash128 GetShapeHash()
        {
            var hash = new Hash128();
            Append(ref hash, _SliceMin);
            Append(ref hash, _SliceMax);
            Append(ref hash, _Bounds.min);
            Append(ref hash, _Bounds.max);
            hash.Append(_Anchor.GetHashCode());
            return hash;
        }

        // Millimeter steps for cache keys
        public static void Append(ref Hash128 hash, Vector3 value, float step = 0.001f)
        {
            hash.Append(Mathf.RoundToInt(value.x / step));
            hash.Append(Mathf.RoundToInt(value.y / step));
            hash.Append(Mathf.RoundToInt(value.z / step));
        }
    }
}
