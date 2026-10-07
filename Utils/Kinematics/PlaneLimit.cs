// Author: František Holubec
// Created: 07.10.2026

using System;
using UnityEngine;

namespace EDIVE.Utils.Kinematics
{
    [Serializable]
    public struct PlaneLimit
    {
        [Tooltip("Plane through this transform. Normal = forward.")]
        [SerializeField]
        private Transform _Plane;

        [Tooltip("Min distance in front of plane.")]
        [SerializeField]
        private float _Offset;

        public bool IsInFront(Vector3 point, Transform mover, Matrix4x4 moverToPose)
        {
            if (!_Plane)
                return true;

            var planePoint = _Plane.position;
            var normal = _Plane.forward;
            if (mover && _Plane.IsChildOf(mover))
            {
                planePoint = moverToPose.MultiplyPoint3x4(planePoint);
                normal = moverToPose.MultiplyVector(normal).normalized;
            }

            return Vector3.Dot(point - planePoint, normal) >= _Offset;
        }
    }
}
