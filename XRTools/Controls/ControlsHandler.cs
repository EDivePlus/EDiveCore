// Author: František Holubec
// Created: 07.05.2025

using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    public class ControlsHandler : MonoBehaviour
    {
        [SerializeField]
        private Transform _Head;
        [SerializeField]
        private Transform _LeftHand;
        [SerializeField]
        private Transform _RightHand;
        
        [SerializeField]
        private Transform _HeadTargetIK;
        [SerializeField]
        private Transform _LeftHandTargetIK;
        [SerializeField]
        private Transform _RightHandTargetIK;

        public Transform Head => _Head;
        public Transform LeftHand => _LeftHand;
        public Transform RightHand => _RightHand;
        public Transform HeadTargetIK => _HeadTargetIK;
        public Transform LeftHandTargetIK => _LeftHandTargetIK;
        public Transform RightHandTargetIK => _RightHandTargetIK;
    }
}
