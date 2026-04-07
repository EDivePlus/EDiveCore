// Author: František Holubec
// Created: 11.03.2026

using System;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [Serializable]
    public class BoundingBoxColliderComponent : AMeshSliceScalerComponent
    {
        [SerializeField]
        private BoxCollider _BoxCollider;
            
        public override string Label => "Bounding Box Collider";
        
        public override bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            return false;
        }
        
        public override void Postprocess(MeshSliceScaleDetails details, Component container, Transform root)
        {
            UpdateCollider(details.Bounds.center, details.Bounds.size.MultiplyElementWise(details.TargetScale), root);
        }

        private void UpdateCollider(Vector3 center, Vector3 size, Transform root)
        {
            if (_BoxCollider == null || root == null)
                return;
            
            if (_BoxCollider.transform != root)
            {
                if (_BoxCollider.transform.position != root.position) 
                    _BoxCollider.transform.position = root.position;

                if (_BoxCollider.transform.rotation != root.rotation) 
                    _BoxCollider.transform.rotation = root.rotation;
            }
            
            if (_BoxCollider.center != center) 
                _BoxCollider.center = center;

            if (_BoxCollider.size != size) 
                _BoxCollider.size = size;
        }
    }
}
