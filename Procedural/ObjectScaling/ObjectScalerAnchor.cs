// Author: František Holubec
// Created: 19.10.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    // Keeps the object pinned to a point of the scaled bounds. Moves only, never scales.
    [ExecuteAlways]
    public class ObjectScalerAnchor : MonoBehaviour
    {
        [SerializeField]
        [Required]
        [OnValueChanged(nameof(Apply))]
        private ObjectScaler _Scaler;

        [SerializeField]
        [Tooltip("Point in the bounds per axis, -1 min, 0 center, 1 max.")]
        [OnValueChanged(nameof(Apply))]
        private Vector3 _Point;

        [SerializeField]
        [Tooltip("In scaler local space.")]
        [OnValueChanged(nameof(Apply))]
        private Vector3 _Offset;

        public Vector3 Point
        {
            get => _Point;
            set
            {
                _Point = value;
                Apply();
            }
        }

        public Vector3 Offset
        {
            get => _Offset;
            set
            {
                _Offset = value;
                Apply();
            }
        }

        private void OnEnable()
        {
            if (_Scaler != null)
                _Scaler.SizeChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            if (_Scaler != null)
                _Scaler.SizeChanged -= Apply;
        }

        public void Apply()
        {
            if (_Scaler == null)
                return;

            var bounds = _Scaler.ScaledBounds;
            var local = bounds.center + Vector3.Scale(bounds.extents, _Point) + _Offset;
            var position = _Scaler.transform.TransformPoint(local);
            if (transform.position != position)
                transform.position = position;
        }

        private void Reset()
        {
            _Scaler = GetComponentInParent<ObjectScaler>();
        }
    }
}
