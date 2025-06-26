// Author: František Holubec
// Created: 26.06.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures
{
    [Serializable]
    [InlineProperty]
    public class TransformSnapshot : IEquatable<TransformSnapshot>
    {
        [SerializeField]
        [LabelWidth(60)]
        private Vector3 _Position = Vector3.zero;

        [SerializeField]
        [LabelWidth(60)]
        private Quaternion _Rotation = Quaternion.identity;

        [SerializeField]
        [LabelWidth(60)]
        private Vector3 _Scale = Vector3.one;

        public Quaternion Rotation => _Rotation;
        public Vector3 Position => _Position;
        public Vector3 Scale => _Scale;

        public TransformSnapshot() { }

        public TransformSnapshot(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _Position = position;
            _Rotation = rotation;
            _Scale = scale;
        }

        public TransformSnapshot(Transform transform)
        {
            FromTransform(transform);
        }

        public void FromTransform(Transform transform)
        {
            _Position = transform.localPosition;
            _Rotation = transform.localRotation;
            _Scale = transform.localScale;
        }

        public void ApplyTo(Transform transform)
        {
            transform.localPosition = _Position;
            transform.localRotation = _Rotation;
            transform.localScale = _Scale;
        }

        public bool Equals(TransformSnapshot other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _Position.Equals(other._Position) && _Rotation.Equals(other._Rotation) && _Scale.Equals(other._Scale);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TransformSnapshot) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_Position, _Rotation, _Scale);
        }
    }
}
