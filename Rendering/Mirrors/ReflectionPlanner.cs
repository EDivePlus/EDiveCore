using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Rendering.Mirrors
{
    public struct ReflectionStep
    {
        public MirrorSurface Surface { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }
        public Matrix4x4 WorldToCameraMatrix { get; set; }
        public Matrix4x4 CullingMatrix { get; set; }

        // Unity convention, not GPU. GetGPUProjectionMatrix flips Y for the target and a UV must not.
        public Matrix4x4 ViewProjection { get; set; }

        public Vector3 CameraPosition { get; set; }
        public int Depth { get; set; }
        public float Distance { get; set; }
        public bool InvertCulling { get; set; }

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
            Camera.MonoOrStereoscopicEye cullEye,
            float viewMargin)
        {
            _steps.Clear();
            if (surfaces == null || surfaces.Count == 0 || profile == null)
                return;

            Walk(renderCamera, reflectionCamera, surfaces, profile, cullEye, viewMargin, 1, null, reflectionCamera.projectionMatrix);
        }

        private void Walk(
            Camera renderCamera,
            Camera reflectionCamera,
            IReadOnlyList<MirrorSurface> surfaces,
            MirrorProfile profile,
            Camera.MonoOrStereoscopicEye cullEye,
            float viewMargin,
            int depth,
            MirrorSurface parentSurface,
            Matrix4x4 baseProjection)
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

                // Recursive darkening goes past the distance limit on purpose. The first bounce never does.
                if (distance > surface.RenderDistance && (depth == 1 || !surface.UseDepthFalloff))
                {
                    _steps.Add(BuildFallbackStep(surface, depth, distance));
                    continue;
                }

                // Offset moves the virtual eye too, sliding the mirror's own edges out of the reflection.
                var planeDistance = -Vector3.Dot(mirrorNormal, mirrorPos) - surface.ClippingPlaneOffset;
                var reflection = CalculateReflectionMatrix(new Vector4(mirrorNormal.x, mirrorNormal.y, mirrorNormal.z, planeDistance));

                var newEyePos = reflection.MultiplyPoint(eyePosition);
                var newForward = Vector3.Reflect(reflectionCamera.transform.forward, mirrorNormal);
                var newUp = Vector3.Reflect(reflectionCamera.transform.up, mirrorNormal);

                // Camera is scratch space for the next level.
                reflectionCamera.transform.SetPositionAndRotation(newEyePos, Quaternion.LookRotation(newForward, newUp));

                var newWorldToCamera = worldToCamera * reflection;
                reflectionCamera.worldToCameraMatrix = newWorldToCamera;

                // Parent left its oblique matrix here. Stacking another on it bends the far plane.
                reflectionCamera.projectionMatrix = baseProjection;

                // Frustum that just covers the mirror. Culling only.
                var tightView = Matrix4x4.identity;
                var tightProjection = Matrix4x4.identity;
                var hasTight = !renderCamera.orthographic
                               && surface.TryGetCullingMatrices(reflectionCamera, profile, cullEye, viewMargin,
                                   out tightView, out tightProjection);

                // Clip behind the mirror. Children walk from this pair, so the winding rule holds.
                var clipPlane = CameraSpacePlane(newWorldToCamera, mirrorPos, mirrorNormal, surface.ClippingPlaneOffset);
                var obliqueProjection = reflectionCamera.CalculateObliqueMatrix(clipPlane);
                reflectionCamera.projectionMatrix = obliqueProjection;

                // Never the oblique one. Its near plane sits on the glass and swallows CullingNearOffset.
                var cullingMatrix = renderCamera.orthographic
                    ? obliqueProjection * newWorldToCamera
                    : hasTight ? tightProjection * tightView : baseProjection * newWorldToCamera;

                var step = new ReflectionStep
                {
                    Surface = surface,
                    ProjectionMatrix = obliqueProjection,
                    WorldToCameraMatrix = newWorldToCamera,
                    CullingMatrix = cullingMatrix,
                    ViewProjection = obliqueProjection * newWorldToCamera,
                    CameraPosition = newEyePos,
                    Depth = depth,
                    Distance = distance,
                    // View is mirrored, so the winding flips on every odd bounce.
                    InvertCulling = depth % 2 != 0
                };

                Walk(renderCamera, reflectionCamera, surfaces, profile, cullEye, viewMargin, depth + 1, surface, baseProjection);

                RestoreScratch(reflectionCamera, eyePosition, eyeRotation, worldToCamera, projection);

                // After the children. Deepest draws first, shallowest wins the material.
                _steps.Add(step);
            }
        }

        // Real depth, so the viewer's own binding still wins over a deeper one.
        private static ReflectionStep BuildFallbackStep(MirrorSurface surface, int depth, float distance)
        {
            return new ReflectionStep
            {
                Surface = surface,
                Depth = depth,
                Distance = distance,
                BeyondRange = true
            };
        }

        private static void RestoreScratch(Camera reflectionCamera, Vector3 position, Quaternion rotation,
            Matrix4x4 worldToCamera, Matrix4x4 projection)
        {
            reflectionCamera.transform.SetPositionAndRotation(position, rotation);
            reflectionCamera.worldToCameraMatrix = worldToCamera;
            reflectionCamera.projectionMatrix = projection;
        }

        // Mirror plane in camera space. What CalculateObliqueMatrix wants.
        private static Vector4 CameraSpacePlane(Matrix4x4 worldToCamera, Vector3 position, Vector3 normal, float offset)
        {
            var offsetPos = position + normal * offset;
            var cameraPos = worldToCamera.MultiplyPoint(offsetPos);
            var cameraNormal = worldToCamera.MultiplyVector(normal).normalized;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPos, cameraNormal));
        }

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
