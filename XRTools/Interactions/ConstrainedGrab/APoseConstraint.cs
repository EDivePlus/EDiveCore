// Author: František Holubec
// Created: 07.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.XRTools.Interactions.ConstrainedGrab
{
    [Serializable]
    [EnhancedTypeSelector]
    public abstract class APoseConstraint
    {
        public abstract bool IsValid(Transform mover, Pose pose);

        public static Matrix4x4 MoverToPose(Transform mover, Pose pose)
        {
            return Matrix4x4.TRS(pose.position, pose.rotation.normalized, mover.lossyScale) * mover.worldToLocalMatrix;
        }
    }
}
