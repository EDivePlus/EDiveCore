// Author: František Holubec
// Created: 13.06.2026

using UnityEngine;

namespace EDIVE.Avatars
{
    public class VRBodySolverRigFollow : ARigFollow
    {
        [SerializeField]
        private VRBodySolver _BodySolver;
        
        public override Transform HeadSource
        {
            get => _BodySolver != null ? _BodySolver.HeadTarget.Target : null;
            set
            {
                if (_BodySolver != null) 
                    _BodySolver.HeadTarget.Target = value;
            }
        }

        public override Transform LeftHandSource
        {
            get => _BodySolver != null ? _BodySolver.LeftHandTarget.Target : null;
            set
            {
                if (_BodySolver != null) 
                    _BodySolver.LeftHandTarget.Target = value;
            }
        }

        public override Transform RightHandSource
        {
            get => _BodySolver != null ? _BodySolver.RightHandTarget.Target : null;
            set
            {
                if (_BodySolver != null) 
                    _BodySolver.RightHandTarget.Target = value;
            }
        }

        public override void Refresh()
        {
            if (_BodySolver != null)
                _BodySolver.RequestRecalibration();
        }
    }
}
