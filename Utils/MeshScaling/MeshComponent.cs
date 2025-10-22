// Author: František Holubec
// Created: 22.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.MeshScaling
{
    [Serializable]
    public class MeshComponent : AMeshScalerComponent
    {
        [SerializeField]
        private Mesh _OriginalMesh;
        
        [ReadOnly]
        [SerializeField]
        private Mesh _SlicedMesh;
        
        [SerializeField]
        private MeshFilter _MeshFilter;
        
        [SerializeField]
        private MeshCollider _MeshCollider;
        
        public void Initialize()
        {
            if (_OriginalMesh == null) 
                return;
            
            _SlicedMesh = UnityEngine.Object.Instantiate(_OriginalMesh);
            _SlicedMesh.name = $"{_OriginalMesh.name}(sliced)";
            if (_MeshFilter != null) 
                _MeshFilter.sharedMesh = _SlicedMesh;
            if (_MeshCollider != null)
                _MeshCollider.sharedMesh = _SlicedMesh;
        }
        
        public bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            if (_OriginalMesh == null)
            {
                bounds = default;
                return false;
            }

            bounds = MeshSlicingUtility.CalculateBounds(_OriginalMesh, _MeshFilter.transform, root);
            return true;
        }
        
        public void RecalculateSlicedMesh(MeshScalerDetails details)
        {
            if (_OriginalMesh == null || _SlicedMesh == null) 
                return;
            
            MeshSlicingUtility.SliceMesh(_OriginalMesh, _SlicedMesh, details.TargetSize, _OriginalMesh.bounds, details.SliceStart, details.SliceEnd);
        }
    }
}
