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
        
        [SerializeField]
        private MeshFilter _MeshFilter;
        
        [SerializeField]
        private MeshCollider _MeshCollider;
        
        [SerializeField]
        private bool _ContributeToBounds = true;
        
        [ShowInInspector]
        private Mesh _targetMesh;
        
        private int _slicedMeshHash;
        private int _modificationsHash;
        
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

        public override void SetPreviewOriginal(bool preview)
        {
            var targetMesh = preview ? _OriginalMesh : _targetMesh;
            if (_MeshFilter != null) 
                _MeshFilter.sharedMesh = targetMesh;
            if (_MeshCollider != null)
                _MeshCollider.sharedMesh = targetMesh;
        }
        
        public override bool Recalculate(MeshSliceScaleDetails details, Component container, Transform root, bool force = false)
        {
            if (_OriginalMesh == null)
            {
                if (_slicedMeshHash != 0)
                {
                    _slicedMeshHash = 0;
                    _modificationsHash = 0;
                    return true;
                }
                return false;
            }

            if (!TryGetTransform(out var tr)) 
                return false;

            var modified = false;
            var newHash = HashCode.Combine(container, _OriginalMesh);
            if (_targetMesh == null || newHash != _slicedMeshHash || force)
            {
                _targetMesh = UnityEngine.Object.Instantiate(_OriginalMesh);
                _targetMesh.name = $"{_OriginalMesh.name} (sliced)";
                _slicedMeshHash = newHash;
                modified = true;
            }
            
            var newModsHash = HashCode.Combine(_targetMesh, details);
            if (newModsHash != _modificationsHash || force)
            {
                MeshUtility.SliceScaleMesh(_OriginalMesh, _targetMesh, details.Bounds.size, details.TargetScale, details.SliceMin, details.SliceMax, tr, root);
                _modificationsHash = newModsHash;
                modified = true;
            }

            if (_MeshFilter != null) 
                _MeshFilter.sharedMesh = _targetMesh;

            if (_MeshCollider != null) 
                _MeshCollider.sharedMesh = _targetMesh;
            
            return  modified;
        }
    }
}
