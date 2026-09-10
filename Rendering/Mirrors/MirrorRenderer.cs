using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace EDIVE.Rendering.Mirrors
{
    public enum MirrorStereoMode
    {
        None,
        SinglePass,
        MultiPass
    }

    public enum MirrorFlip
    {
        UseRules,
        Never,
        Always
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MirrorRenderer : MonoBehaviour
    {
        [SerializeField]
        [Required]
        [EnhancedInlineEditor]
        private MirrorProfile _Profile;

        [SerializeField]
        [Tooltip("List every surface that can see another one.")]
        [ValidateInput(nameof(ValidateSurfaces), "The list contains empty entries.")]
        private List<MirrorSurface> _Surfaces = new();

        [SerializeField]
        [Tooltip("Stops rendering. Blending keeps running. Disable the component for a full stop.")]
        private bool _PauseRendering;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Some setups render the reflection upside down. Use Rules reads the profile table.")]
        private MirrorFlip _Flip = MirrorFlip.UseRules;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Also render in the scene view. Both views share one material and will fight.")]
        private bool _IncludeSceneView;

        private MirrorResources _resources;
        private ReflectionPlanner _planner;
        private readonly Dictionary<Camera, int> _frameSkip = new();
        private readonly List<Camera> _deadSkipKeys = new();
        private MirrorProfile _boundProfile;
        private int _pruneCounter;
        private bool _rendering;
        private bool _preallocated;

        public IReadOnlyList<MirrorSurface> Surfaces => _Surfaces;
        public MirrorProfile Profile => _Profile;

        private void OnEnable()
        {
            Setup();
        }

        private void OnDisable()
        {
            Teardown();
        }
        
        [PropertySpace]
        [Button]
        public void Rebuild()
        {
            Teardown();

            if (isActiveAndEnabled)
                Setup();
        }

        private void Setup()
        {
            Teardown();
            _resources = new MirrorResources(name);
            _planner = new ReflectionPlanner();

            BindProfile();

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void Teardown()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            UnbindProfile();

            // Show the fallback, not a frozen frame.
            foreach (var surface in _Surfaces)
            {
                if (surface != null)
                    surface.ShowFallback();
            }

            _resources?.Dispose();
            _resources = null;
            _planner = null;
            _rendering = false;
            _preallocated = false;
            _pruneCounter = 0;
            _frameSkip.Clear();
        }

        private void BindProfile()
        {
            UnbindProfile();
            if (_Profile == null)
                return;

            _boundProfile = _Profile;
            _boundProfile.Changed += OnProfileChanged;
        }

        private void UnbindProfile()
        {
            if (_boundProfile == null)
                return;

            _boundProfile.Changed -= OnProfileChanged;
            _boundProfile = null;
        }

        private void OnProfileChanged()
        {
            _resources?.ReleaseTextures();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderCamera)
        {
            if (_Profile == null || _resources == null || renderCamera == null || _Surfaces.Count == 0)
                return;
            if (_rendering || !ShouldServe(renderCamera))
                return;

            // Visible but too far. Still needs a blend update or it freezes.
            var viewer = renderCamera.transform.position;
            var anyVisible = false;
            foreach (var surface in _Surfaces)
            {
                if (surface == null || !surface.IsVisibleFrom(renderCamera, true))
                    continue;

                var distance = Vector3.Distance(viewer, surface.ClosestPoint(viewer));
                if (distance > surface.RenderDistance)
                {
                    surface.SetBlend(surface.CalculateBlend(1, _Profile.Recursions, distance));
                    continue;
                }

                anyVisible = true;
            }

            if (!anyVisible)
                return;

            if (++_pruneCounter >= 120)
            {
                _pruneCounter = 0;
                _resources.Prune();
                PruneFrameSkip();
            }

            if (_PauseRendering)
            {
                UpdateBlendsOnly(renderCamera);
                return;
            }

            if (!_preallocated)
            {
                _preallocated = true;
                _resources.Preallocate(_Profile, renderCamera, _Profile.PreallocatedTextures);
            }

            var reflectionCamera = _resources.GetReflectionCamera(renderCamera, _Profile);
            PrepareReflectionCamera(renderCamera, reflectionCamera);

            var stereoMode = GetStereoMode(renderCamera);
            var shouldRender = ConsumeFrameSkip(renderCamera);

            _rendering = true;
            try
            {
                switch (stereoMode)
                {
                    case MirrorStereoMode.SinglePass:
                        // One pass draws both eyes. Both textures must be ready.
                        RenderEye(context, renderCamera, reflectionCamera, Camera.StereoscopicEye.Left, true, shouldRender);
                        RenderEye(context, renderCamera, reflectionCamera, Camera.StereoscopicEye.Right, true, shouldRender);
                        SetForceEyeOnAll(-1);
                        break;

                    case MirrorStereoMode.MultiPass:
                        // One eye per pass. Matrices are already set.
                        var eye = renderCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right
                            ? Camera.StereoscopicEye.Right
                            : Camera.StereoscopicEye.Left;
                        RenderEye(context, renderCamera, reflectionCamera, eye, false, shouldRender);
                        SetForceEyeOnAll((int) eye);
                        break;

                    case MirrorStereoMode.None:
                    default:
                        RenderEye(context, renderCamera, reflectionCamera, Camera.StereoscopicEye.Left, false, shouldRender);
                        SetForceEyeOnAll(0);
                        break;
                }
            }
            finally
            {
                _rendering = false;
            }

        }

        private void RenderEye(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera,
            Camera.StereoscopicEye eye, bool stereo, bool shouldRender)
        {
            if (stereo)
            {
                reflectionCamera.worldToCameraMatrix = renderCamera.GetStereoViewMatrix(eye);
                reflectionCamera.projectionMatrix = renderCamera.GetStereoProjectionMatrix(eye);
                var view = reflectionCamera.worldToCameraMatrix.inverse;
                reflectionCamera.transform.SetPositionAndRotation(view.GetColumn(3), view.rotation);
            }
            else
            {
                reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position, renderCamera.transform.rotation);
                reflectionCamera.worldToCameraMatrix = renderCamera.worldToCameraMatrix;
                reflectionCamera.projectionMatrix = renderCamera.projectionMatrix;
            }

            _planner.Build(renderCamera, reflectionCamera, _Surfaces, _Profile, eye, stereo);
            RenderPlan(context, renderCamera, reflectionCamera, eye, shouldRender);
        }

        private void RenderPlan(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera,
            Camera.StereoscopicEye eye, bool shouldRender)
        {
            var steps = _planner.Steps;
            var recursions = _Profile.Recursions;
            var oldPixelLights = QualitySettings.pixelLightCount;
            var renderScale = UniversalRenderPipeline.asset != null ? UniversalRenderPipeline.asset.renderScale : 1f;

            try
            {
                if (_Profile.DisablePixelLights)
                    QualitySettings.pixelLightCount = 0;

                foreach (var step in steps)
                {
                    var surface = step.Surface;
                    if (surface == null)
                        continue;

                    surface.SetEye(eye);
                    surface.SetFlipY(ShouldFlip(renderCamera, step.Depth));

                    if (step.BeyondRange || step.Depth >= recursions + 1)
                    {
                        surface.ShowFallback();

                        continue;
                    }

                    var pooled = _resources.Acquire(renderCamera, eye, _Profile);

                    var blend = surface.CalculateBlend(step.Depth, recursions, step.Distance);
                    surface.SetReflectionTexture(eye, pooled.Texture);
                    surface.SetBlend(blend);

                    if (shouldRender)
                        DrawStep(context, renderCamera, reflectionCamera, step, pooled.Texture);
                }
            }
            finally
            {
                GL.invertCulling = false;
                QualitySettings.pixelLightCount = oldPixelLights;
                if (UniversalRenderPipeline.asset != null)
                    UniversalRenderPipeline.asset.renderScale = renderScale;

                reflectionCamera.targetTexture = null;
                reflectionCamera.ResetCullingMatrix();
                _resources.ReleaseAll();
            }
        }

        private void DrawStep(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera,
            ReflectionStep step, RenderTexture target)
        {
            reflectionCamera.targetTexture = target;
            reflectionCamera.transform.position = step.CameraPosition;
            reflectionCamera.worldToCameraMatrix = step.WorldToCameraMatrix;
            reflectionCamera.projectionMatrix = step.ProjectionMatrix;
            // cullingMatrix sticks. Only set it when wanted.
            if (_Profile.TightFrustumCulling)
                reflectionCamera.cullingMatrix = step.CullingMatrix;
            else
                reflectionCamera.ResetCullingMatrix();

            // Winding flips on every odd bounce.
            GL.invertCulling = step.InvertCulling;

            if (UniversalRenderPipeline.asset != null)
                UniversalRenderPipeline.asset.renderScale = 1f;

            var isSceneCamera = renderCamera.cameraType == CameraType.SceneView;
            if (!isSceneCamera || _IncludeSceneView)
            {
#pragma warning disable CS0618 // RenderSingleCamera is obsolete. SubmitRenderRequest recurses here.
                UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
#pragma warning restore CS0618
            }

            GL.invertCulling = false;
        }

        private void UpdateBlendsOnly(Camera renderCamera)
        {
            var position = renderCamera.transform.position;
            foreach (var surface in _Surfaces)
            {
                if (surface == null)
                    continue;

                var distance = Vector3.Distance(position, surface.ClosestPoint(position));
                surface.SetBlend(surface.CalculateBlend(1, _Profile.Recursions, distance));
            }
        }

        private void PrepareReflectionCamera(Camera renderCamera, Camera reflectionCamera)
        {
            reflectionCamera.CopyFrom(renderCamera);
            reflectionCamera.enabled = false;
            reflectionCamera.rect = new Rect(0, 0, 1, 1);
            reflectionCamera.cullingMask = _Profile.RenderLayers;
            reflectionCamera.targetTexture = null;

            if (_Profile.OverrideClearFlags)
            {
                reflectionCamera.clearFlags = _Profile.ClearFlags;
                reflectionCamera.backgroundColor = _Profile.ClearColor;
            }

            MirrorResources.Configure(reflectionCamera.GetComponent<UniversalAdditionalCameraData>(), _Profile);

            var skybox = reflectionCamera.GetComponent<Skybox>();
            if (skybox != null)
            {
                if (_Profile.CustomSkybox != null)
                {
                    skybox.material = _Profile.CustomSkybox;
                }
                else
                {
                    var sourceSkybox = renderCamera.GetComponent<Skybox>();
                    skybox.material = sourceSkybox != null ? sourceSkybox.material : RenderSettings.skybox;
                }
            }
        }

        private bool ShouldServe(Camera cam)
        {
            // CopyFrom copies cameraType and tag. Our own camera looks servable.
            if (_resources.IsReflectionCamera(cam))
                return false;

            if (cam.cameraType == CameraType.Reflection || cam.cameraType == CameraType.Preview)
                return false;

            // Preview cameras say SceneView too.
            if (cam.cameraType == CameraType.SceneView)
                return _IncludeSceneView && IsOpenSceneView(cam);

            if (!cam.CompareTag("MainCamera"))
                return false;

            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            return data == null || data.renderType != CameraRenderType.Overlay;
        }

        private static bool IsOpenSceneView(Camera camera)
        {
#if UNITY_EDITOR
            foreach (UnityEditor.SceneView view in UnityEditor.SceneView.sceneViews)
            {
                if (view != null && view.camera == camera)
                    return true;
            }
#endif
            return false;
        }

        private static MirrorStereoMode GetStereoMode(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView || !XRSettings.enabled
                || XRSettings.eyeTextureWidth <= 0 || !camera.stereoEnabled)
                return MirrorStereoMode.None;

            return XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass
                ? MirrorStereoMode.MultiPass
                : MirrorStereoMode.SinglePass;
        }

        private void PruneFrameSkip()
        {
            _deadSkipKeys.Clear();
            foreach (var pair in _frameSkip)
            {
                if (pair.Key == null)
                    _deadSkipKeys.Add(pair.Key);
            }

            foreach (var key in _deadSkipKeys)
                _frameSkip.Remove(key);
        }

        private bool ConsumeFrameSkip(Camera cam)
        {
            var needed = _Profile.UpdateInterval;
            if (needed <= 0)
                return true;

            _frameSkip.TryGetValue(cam, out var remaining);
            if (remaining > 0)
            {
                _frameSkip[cam] = remaining - 1;
                return false;
            }

            _frameSkip[cam] = needed;
            return true;
        }

        private bool ShouldFlip(Camera cam, int depth)
        {
            return _Flip switch
            {
                MirrorFlip.Never => false,
                MirrorFlip.Always => true,
                _ => _Profile.FlipRules != null && _Profile.FlipRules.Evaluate(cam, depth)
            };
        }

        private void SetForceEyeOnAll(int value)
        {
            foreach (var surface in _Surfaces)
                surface?.SetForceEye(value);
        }

        private bool ValidateSurfaces(List<MirrorSurface> surfaces)
        {
            return surfaces == null || surfaces.All(surface => surface != null);
        }
    }
}
