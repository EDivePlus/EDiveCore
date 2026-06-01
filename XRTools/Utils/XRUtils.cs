// Author: František Holubec
// Created: 20.06.2025

using EDIVE.XRTools.DeviceSimulator;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace EDIVE.XRTools
{
    public static class XRUtils
    {
        public static bool XREnabled => XRSettings.enabled || XRDeviceSimulatorUtils.SimulatorEnabled;

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
        
        public static bool TryGetCurrentRaycastTarget(this IXRHoverInteractor interactor, Collider collider, out RaycastHit hit)
        {
            hit = default;

            if (collider == null)
                return false;

            if (interactor is XRRayInteractor rayInteractor)
            {
                if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
                    return hit.collider == collider;

                return false;
            }

            if (interactor is NearFarInteractor nearFarInteractor)
            {
                if (nearFarInteractor.TryGetCurveEndPoint(out var endPoint) == EndPointType.ValidCastHit)
                {
                    var source = nearFarInteractor.attachTransform != null
                        ? nearFarInteractor.attachTransform
                        : nearFarInteractor.transform;

                    var rayDirection = source.forward;
                    if (rayDirection.sqrMagnitude < 0.0001f)
                        return false;

                    rayDirection.Normalize();

                    var rayOrigin = endPoint - rayDirection * 0.1f;
                    return collider.Raycast(new Ray(rayOrigin, rayDirection), out hit, 0.2f);
                }
            }

            return false;
        }
    }
}
