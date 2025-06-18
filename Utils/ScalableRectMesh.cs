// Author: František Holubec
// Created: 18.06.2025

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils
{
    public class ScalableRectMesh : MonoBehaviour
    {
        [SerializeField]
        [OnValueChanged(nameof(UpdateVisual))]
        private Vector2 _Size = Vector2.one;

        [SerializeField]
        [OnValueChanged(nameof(UpdateVisual))]
        private Vector3 _XDirection = Vector3.right;

        [SerializeField]
        [OnValueChanged(nameof(UpdateVisual))]
        private Vector3 _YDirection = Vector3.up;

        [PropertySpace]
        [SerializeField]
        private Transform _TopRightExtend;

        [SerializeField]
        private Transform _TopLeftExtend;

        [SerializeField]
        private Transform _BottomRightExtend;

        [SerializeField]
        private Transform _BottomLeftExtend;

        private void UpdateVisual()
        {
            var halfSize = _Size / 2;
            if (_TopRightExtend) _TopRightExtend.localPosition = _XDirection * halfSize.x + _YDirection * halfSize.y;
            if (_TopLeftExtend) _TopLeftExtend.localPosition = -_XDirection * halfSize.x + _YDirection * halfSize.y;
            if (_BottomRightExtend) _BottomRightExtend.localPosition = _XDirection * halfSize.x - _YDirection * halfSize.y;
            if (_BottomLeftExtend) _BottomLeftExtend.localPosition = -_XDirection * halfSize.x - _YDirection * halfSize.y;
        }
    }
}
