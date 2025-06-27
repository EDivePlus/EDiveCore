// Author: František Holubec
// Created: 20.06.2025

using EDIVE.XRTools.DeviceSimulator;
using UnityEngine;

namespace EDIVE.XRTools
{
    public static class XRUtils
    {
        public static bool XREnabled => UnityEngine.XR.XRSettings.enabled || XRDeviceSimulatorUtils.SimulatorEnabled;

        public static bool IsTargetInView(Transform source, Transform target, float maxDistance = 2f, float frontThreshold = 0.5f, float facingThreshold = 0.5f)
        {
            var toTarget = target.position - source.position;
            var distance = toTarget.magnitude;

            // Check distance
            if (distance > maxDistance)
                return false;

            // Normalize direction
            var toTargetDir = toTarget.normalized;

            // Check if the target is in front of the source
            var forwardDot = Vector3.Dot(source.forward, toTargetDir);
            if (forwardDot < 1f - frontThreshold)
                return false;

            // Check if target is facing the source
            var facingDot = Vector3.Dot(target.forward, source.forward);
            return facingDot > 1f - facingThreshold;
        }
    }
}
