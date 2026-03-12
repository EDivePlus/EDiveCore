// Author: František Holubec
// Created: 22.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [Serializable]
    public class MeshComponent : AMeshSliceScalerComponent
    {
        [SerializeField]
        private Mesh _OriginalMesh;
        
        [ReadOnly]
        [SerializeField]
        private Mesh _TargetMesh;
        
        [SerializeField]
        private MeshFilter _MeshFilter;
        
        [SerializeField]
        private MeshCollider _MeshCollider;
        
        [SerializeField]
        private bool _ContributeToBounds = true;
        
        [HideInInspector]
        [SerializeField]
        private int _SlicedMeshHash;
        
        [HideInInspector]
        [SerializeField]
        private int _ModificationsHash;
        
        public override string Label => "Mesh";

        public override bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (!_ContributeToBounds || _OriginalMesh == null)
                return false;

            if (TryGetTransform(out var tr))
            {
                bounds = MeshUtility.CalculateBounds(_OriginalMesh, tr, root);
                return true;
            }
            return false;
        }

        private bool TryGetTransform(out Transform root)
        {
            root = null;
            if (_MeshFilter != null)
            {
                root = _MeshFilter.transform;
                return true;
            }

            if (_MeshCollider != null)
            {
                root = _MeshCollider.transform;
                return true;
            }

            return false;
        }

        public override void PreviewOriginal(MeshSliceScaleDetails details, Component container, Transform root)
        {
            _SlicedMeshHash = 0;
            _ModificationsHash = 0;
            if (_MeshFilter != null) 
                _MeshFilter.sharedMesh = _OriginalMesh;
            if (_MeshCollider != null)
                _MeshCollider.sharedMesh = _OriginalMesh;
        }
        
        public override bool Recalculate(MeshSliceScaleDetails details, Component container, Transform root, bool force = false)
        {
            if (_OriginalMesh == null)
            {
                if (_SlicedMeshHash != 0)
                {
                    _SlicedMeshHash = 0;
                    _ModificationsHash = 0;
                    return true;
                }
                return false;
            }

            if (!TryGetTransform(out var tr)) 
                return false;

            var modified = false;
            var newHash = HashCode.Combine(container, _OriginalMesh);
            if (_TargetMesh == null || newHash != _SlicedMeshHash || force)
            {
                _TargetMesh = UnityEngine.Object.Instantiate(_OriginalMesh);
                _TargetMesh.name = $"{_OriginalMesh.name} (sliced)";
                _SlicedMeshHash = newHash;
                modified = true;
            }
            
            var newModsHash = HashCode.Combine(_OriginalMesh, details);
            if (newModsHash != _ModificationsHash || force)
            {
                MeshUtility.SliceScaleMesh(_OriginalMesh, _TargetMesh, details.Bounds.size, details.TargetScale, details.SliceMin, details.SliceMax, tr, root);
                _ModificationsHash = newModsHash;
                modified = true;
            }
            
            if (_MeshFilter != null) 
                _MeshFilter.sharedMesh = _TargetMesh;
            if (_MeshCollider != null)
                _MeshCollider.sharedMesh = _TargetMesh;
            return  modified;
        }
    }
}
