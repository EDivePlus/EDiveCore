// Author: František Holubec
// Created: 07.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Kinematics;
using UnityEngine;

namespace EDIVE.XRTools.Interactions.ConstrainedGrab
{
    [Serializable]
    public class BoundsPlaneConstraint : APoseConstraint
    {
        [Tooltip("Box kept in front of planes.")]
        [SerializeField]
        private BoxCollider _Bounds;

        [SerializeField]
        [EnhancedTableList(ShowFoldout = false)]
        private PlaneLimit[] _Planes = Array.Empty<PlaneLimit>();

        public override bool IsValid(Transform mover, Pose pose)
        {
            if (!_Bounds)
                return true;

            var moverToPose = MoverToPose(mover, pose);
            var toBounds = _Bounds.transform.IsChildOf(mover) ? moverToPose * _Bounds.transform.localToWorldMatrix : _Bounds.transform.localToWorldMatrix;
            var extents = _Bounds.size * 0.5f;

            for (var i = 0; i < 8; i++)
            {
                var corner = toBounds.MultiplyPoint3x4(_Bounds.center + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z));

                foreach (var plane in _Planes)
                {
                    if (!plane.IsInFront(corner, mover, moverToPose))
                        return false;
                }
            }

            return true;
        }
    }
}
