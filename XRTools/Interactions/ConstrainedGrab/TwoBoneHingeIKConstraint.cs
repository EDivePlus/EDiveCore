// Author: František Holubec
// Created: 07.10.2026

using System;
using EDIVE.Utils.Kinematics;
using UnityEngine;

namespace EDIVE.XRTools.Interactions.ConstrainedGrab
{
    [Serializable]
    public class TwoBoneHingeIKConstraint : APoseConstraint
    {
        [SerializeField]
        private TwoBoneHingeIK _IK;

        public override bool IsValid(Transform mover, Pose pose)
        {
            return !_IK || _IK.IsValid(mover, pose);
        }
    }
}
