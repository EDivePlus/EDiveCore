// Author: František Holubec
// Created: 11.03.2026

using System;
using EDIVE.SerializedTypeMigration;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    public enum BoxColliderEdge
    {
        None,
        Left,
        Right,
        Bottom,
        Top
    }

    [Serializable]
    [FormerlySerializedType(0, "BoundingBoxColliderComponent", "EDIVE.Procedural.MeshScaling")]
    public class BoxColliderModule : AObjectScalerModule
    {
        [SerializeField]
        private BoxCollider _BoxCollider;

        [SerializeField]
        [Tooltip("Fit only a strip along this edge of the bounds.")]
        private BoxColliderEdge _Edge;

        [SerializeField]
        [HideIf(nameof(_Edge), BoxColliderEdge.None)]
        [Tooltip("Strip width in meters.")]
        private float _Width = 0.01f;

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

            var bounds = GetEdgeBounds(scaler.ScaledBounds);
            if (_BoxCollider.center != bounds.center)
                _BoxCollider.center = bounds.center;

            if (_BoxCollider.size != bounds.size)
                _BoxCollider.size = bounds.size;
        }

        private Bounds GetEdgeBounds(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            switch (_Edge)
            {
                case BoxColliderEdge.Left:
                    max.x = Mathf.Min(max.x, min.x + _Width);
                    break;
                case BoxColliderEdge.Right:
                    min.x = Mathf.Max(min.x, max.x - _Width);
                    break;
                case BoxColliderEdge.Bottom:
                    max.y = Mathf.Min(max.y, min.y + _Width);
                    break;
                case BoxColliderEdge.Top:
                    min.y = Mathf.Max(min.y, max.y - _Width);
                    break;
            }

            bounds.SetMinMax(min, max);
            return bounds;
        }
    }
}
