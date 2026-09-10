using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Rendering.Mirrors
{
    // One mirror to draw, at one depth.
    public struct ReflectionStep
    {
        public MirrorSurface Surface { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }
        public Matrix4x4 WorldToCameraMatrix { get; set; }
        public Matrix4x4 CullingMatrix { get; set; }
        public Vector3 CameraPosition { get; set; }
        public int Depth { get; set; }
        public float Distance { get; set; }
        public bool InvertCulling { get; set; }

        // Too far. Just blend it out.
        public bool BeyondRange { get; set; }
    }

    public class ReflectionPlanner
    {
        private readonly List<ReflectionStep> _steps = new(16);

        public IReadOnlyList<ReflectionStep> Steps => _steps;

        public void Build(
            Camera renderCamera,
            Camera reflectionCamera,
            IReadOnlyList<MirrorSurface> surfaces,
            MirrorProfile profile,
            Camera.StereoscopicEye eye,
            bool stereoActive)
        {
            _steps.Clear();
            if (surfaces == null || surfaces.Count == 0 || profile == null)
                return;

            Walk(renderCamera, reflectionCamera, surfaces, profile, eye, stereoActive, 1, null);
        }

        private void Walk(
            Camera renderCamera,
            Camera reflectionCamera,
            IReadOnlyList<MirrorSurface> surfaces,
            MirrorProfile profile,
            Camera.StereoscopicEye eye,
            bool stereoActive,
            int depth,
            MirrorSurface parentSurface)
        {
            // One past the limit, so the last ring can blend out.
            if (depth > profile.Recursions + 1)
                return;

            var eyePosition = reflectionCamera.transform.position;
            var eyeRotation = reflectionCamera.transform.rotation;
            var worldToCamera = reflectionCamera.worldToCameraMatrix;
            var projection = reflectionCamera.projectionMatrix;

            foreach (var surface in surfaces)
            {
                if (surface == null || surface == parentSurface)
                    continue;
                if (!surface.IsVisibleFrom(reflectionCamera, true))
                    continue;

                var mirrorPos = surface.ForwardTransform.position;
                var mirrorNormal = -surface.ForwardTransform.forward;
                var distance = Vector3.Distance(eyePosition, surface.ClosestPoint(eyePosition));

                // Recursive darkening goes past the distance limit on purpose.
                if (distance > surface.RenderDistance && !surface.UseDepthFalloff)
                {
                    _steps.Add(new ReflectionStep
                    {
                        Surface = surface,
                        Depth = profile.Recursions + 1,
                        Distance = distance,
                        BeyondRange = true
                    });
                    continue;
                }

                var planeDistance = -Vector3.Dot(mirrorNormal, mirrorPos) - surface.ClippingPlaneOffset;
                var reflection = CalculateReflectionMatrix(new Vector4(mirrorNormal.x, mirrorNormal.y, mirrorNormal.z, planeDistance));

                var newEyePos = reflection.MultiplyPoint(eyePosition);
                var newForward = Vector3.Reflect(reflectionCamera.transform.forward, mirrorNormal);

                // Camera is scratch space for the next level.
                reflectionCamera.transform.SetPositionAndRotation(newEyePos, Quaternion.LookRotation(newForward));

                var newWorldToCamera = worldToCamera * reflection;
                reflectionCamera.worldToCameraMatrix = newWorldToCamera;

                // Projection does not depend on the view. The entry value stays good.
                var cullingMatrix = projection * newWorldToCamera;

                if (profile.TightFrustumCulling && !renderCamera.orthographic)
                {
                    var monoEye = stereoActive ? (Camera.MonoOrStereoscopicEye) eye : Camera.MonoOrStereoscopicEye.Mono;
                    // Nothing to cull to. Keep the wide frustum.
                    if (surface.TryGetCullingMatrix(reflectionCamera, profile, monoEye, out var tight))
                        cullingMatrix = tight;
                }

                // Clip everything behind the mirror plane.
                var clipPlane = CameraSpacePlane(newWorldToCamera, mirrorPos, mirrorNormal, surface.ClippingPlaneOffset);
                var obliqueProjection = reflectionCamera.CalculateObliqueMatrix(clipPlane);
                reflectionCamera.projectionMatrix = obliqueProjection;

                if (profile.TightFrustumCulling && renderCamera.orthographic)
                    cullingMatrix = obliqueProjection * newWorldToCamera;

                _steps.Add(new ReflectionStep
                {
                    Surface = surface,
                    ProjectionMatrix = obliqueProjection,
                    WorldToCameraMatrix = newWorldToCamera,
                    CullingMatrix = cullingMatrix,
                    CameraPosition = newEyePos,
                    Depth = depth,
                    Distance = distance,
                    InvertCulling = depth % 2 != 0
                });

                Walk(renderCamera, reflectionCamera, surfaces, profile, eye, stereoActive, depth + 1, surface);

                // Put the scratch state back.
                reflectionCamera.transform.SetPositionAndRotation(eyePosition, eyeRotation);
                reflectionCamera.worldToCameraMatrix = worldToCamera;
                reflectionCamera.projectionMatrix = projection;
            }
        }

        // Mirror plane in camera space. What CalculateObliqueMatrix wants.
        private static Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 position, Vector3 normal, float offset)
        {
            var offsetPos = position + normal * offset;
            var cameraPos = worldToCamera.MultiplyPoint(offsetPos);
            var cameraNormal = worldToCamera.MultiplyVector(normal).normalized;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPos, cameraNormal));
        }

        // Mirrors a point across the plane.
        private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            var m = Matrix4x4.zero;

            m.m00 = 1f - 2f * plane.x * plane.x;
            m.m01 = -2f * plane.x * plane.y;
            m.m02 = -2f * plane.x * plane.z;
            m.m03 = -2f * plane.w * plane.x;

            m.m10 = -2f * plane.y * plane.x;
            m.m11 = 1f - 2f * plane.y * plane.y;
            m.m12 = -2f * plane.y * plane.z;
            m.m13 = -2f * plane.w * plane.y;

            m.m20 = -2f * plane.z * plane.x;
            m.m21 = -2f * plane.z * plane.y;
            m.m22 = 1f - 2f * plane.z * plane.z;
            m.m23 = -2f * plane.w * plane.z;

            m.m33 = 1f;
            return m;
        }
    }
}
