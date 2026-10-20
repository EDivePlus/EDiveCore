// Author: František Holubec
// Created: 11.03.2026

using System;
using EDIVE.SerializedTypeMigration;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    [Serializable]
    [FormerlySerializedType(0, "BoundingBoxColliderComponent", "EDIVE.Procedural.MeshScaling")]
    public class BoxColliderModule : AObjectScalerModule
    {
        [SerializeField]
        private BoxCollider _BoxCollider;

        public override string Label => "Box Collider";

        public override void Postprocess(ObjectScaler scaler)
        {
            if (_BoxCollider == null)
                return;

            var root = scaler.transform;
            var colliderTransform = _BoxCollider.transform;
            if (colliderTransform != root)
            {
                if (colliderTransform.position != root.position)
                    colliderTransform.position = root.position;

                if (colliderTransform.rotation != root.rotation)
                    colliderTransform.rotation = root.rotation;
            }

            var bounds = scaler.ScaledBounds;
            if (_BoxCollider.center != bounds.center)
                _BoxCollider.center = bounds.center;

            if (_BoxCollider.size != bounds.size)
                _BoxCollider.size = bounds.size;
        }
    }
}
