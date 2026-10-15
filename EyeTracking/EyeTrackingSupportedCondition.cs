// Author: Michal Petr
// Created: 15.10.2026

using System;
using EDIVE.Conditions;

namespace EDIVE.EyeTracking
{
    [Serializable]
    public class EyeTrackingSupportedCondition : ABoolCondition
    {
#if OCULUS_VR
        protected override bool GetValue() => OVRPlugin.eyeTrackingSupported;
#else
        protected override bool GetValue() => false;
#endif
    }
}
