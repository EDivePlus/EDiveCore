#if XR_HANDS

// Author: František Holubec
// Created: 17.03.2026

using UnityEngine.XR.Hands;

namespace EDIVE.XRTools.HandGestures
{
    public interface IHandGesture
    {
        bool CheckValid();
        bool CheckConditions(XRHandJointsUpdatedEventArgs eventArgs);
    }
}
#endif