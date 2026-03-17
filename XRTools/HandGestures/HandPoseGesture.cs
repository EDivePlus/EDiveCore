#if XR_HANDS

// Author: František Holubec
// Created: 17.03.2026

using System;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

namespace EDIVE.XRTools.HandGestures
{
    [Serializable]
    public class HandPoseGesture : IHandGesture
    {
        [SerializeField]
        private XRHandPose _HandPose;
        
        [SerializeField]
        private Transform _Target;
        
        public bool CheckValid() => _HandPose != null;
        public bool CheckConditions(XRHandJointsUpdatedEventArgs eventArgs)
        {
            _HandPose.relativeOrientation.targetTransform = _Target;
            return _HandPose.CheckConditions(eventArgs);
        }
    }
}
#endif