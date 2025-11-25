// Author: František Holubec
// Created: 22.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [Serializable]
    public class MeshSliceScaleDetails
    {
        [SerializeField]
        private Vector3 _TargetScale = Vector3.one;

        [SerializeField]
        private Vector3 _SliceMin = Vector3.zero;

        [SerializeField]
        private Vector3 _SliceMax = Vector3.zero;
        
        [Header("Bounds")]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private Bounds _Bounds;

        public ref Vector3 TargetScale => ref _TargetScale;
        public ref Vector3 SliceMin => ref _SliceMin;
        public ref Vector3 SliceMax => ref _SliceMax;
        public ref Bounds Bounds => ref _Bounds;

        protected bool Equals(MeshSliceScaleDetails other)
        {
            return _TargetScale.Equals(other._TargetScale) && _SliceMin.Equals(other._SliceMin) && _SliceMax.Equals(other._SliceMax) && _Bounds.Equals(other._Bounds);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MeshSliceScaleDetails) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_TargetScale, _SliceMin, _SliceMax, _Bounds);
        }
    }
}
