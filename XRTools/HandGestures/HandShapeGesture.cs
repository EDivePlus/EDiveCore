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
    public class HandShapeGesture : IHandGesture
    {
        [SerializeField]
        private XRHandShape _HandShape;

        public bool CheckValid() => _HandShape != null;
        public bool CheckConditions(XRHandJointsUpdatedEventArgs eventArgs)
        {
            return _HandShape.CheckConditions(eventArgs);
        }
    }
}
#endif