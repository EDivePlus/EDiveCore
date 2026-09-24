using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

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

#if UNITY_EDITOR
        [PropertySpace]
        [ShowInInspector]
        [Tooltip("Also render in the scene view. Both views share one material and will fight.")]
        public bool IncludeSceneView
        {
            get => IncludeSceneViewContext.Value;
            set
            {
                if (IncludeSceneViewContext.Value == value)
                    return;

                IncludeSceneViewContext.Value = value;
                UnityEditor.SceneView.RepaintAll();
            }
        }

        private static GlobalPersistentContext<bool> IncludeSceneViewContext => PersistentContext.Get("MirrorRenderer.IncludeSceneView", false);
#endif
        
        private const float SHARED_EYE_EXPAND = 0.1f;
        
        private struct FrameSkipState
        {
            public int Remaining;
            public int Frame;
            public bool Render;
        }
        
        private struct ReflectionBinding
        {
            public int Depth;
            public RenderTexture Texture;
            public Matrix4x4 ViewProjection;
            public float Blend;
            public bool FlipY;
            public bool Fallback;
        }

        private struct Frustum
        {
            public float Left;
            public float Right;
            public float Bottom;
            public float Top;
            public float Near;
            public float Far;
        }

        private MirrorResources _resources;
        private ReflectionPlanner _planner;
        private Material _fadeMaterial;
        private MirrorBackgroundFadePass _fadePass;
        private readonly Dictionary<Camera, FrameSkipState> _frameSkip = new();
        private readonly Dictionary<Camera, int> _sharedFrame = new();
        private readonly Dictionary<MirrorSurface, ReflectionBinding> _finalBindings = new();
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
            _resources = new MirrorResources(name, transform);
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

            if (_fadeMaterial != null)
                CoreUtils.Destroy(_fadeMaterial);
            _fadeMaterial = null;
            _fadePass = null;
            _rendering = false;
            _preallocated = false;
            _pruneCounter = 0;
            _frameSkip.Clear();
            _sharedFrame.Clear();
            _finalBindings.Clear();
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
            _preallocated = false;
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
                PruneCameraKeys();
            }

            if (_PauseRendering)
            {
                UpdateBlendsOnly(renderCamera);
                return;
            }

            var stereoMode = GetStereoMode(renderCamera);
            var sharedCenter = stereoMode != MirrorStereoMode.None && _Profile.SharedCenterEye;

            // Multi pass calls us per eye. Shared only needs the first.
            if (sharedCenter && stereoMode == MirrorStereoMode.MultiPass && !ClaimSharedFrame(renderCamera))
                return;

            if (!_preallocated)
            {
                _preallocated = true;
                _resources.Preallocate(_Profile, renderCamera, _Profile.PreallocatedTextures);
            }

            var reflectionCamera = _resources.GetReflectionCamera(renderCamera, _Profile);
            PrepareReflectionCamera(renderCamera, reflectionCamera);

            var shouldRender = ConsumeFrameSkip(renderCamera);

            _rendering = true;
            try
            {
                if (sharedCenter)
                {
                    RenderCenter(context, renderCamera, reflectionCamera, shouldRender);
                    SetForceEyeOnAll(0);
                }
                else
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
                ApplyViewMatrices(reflectionCamera, renderCamera.GetStereoViewMatrix(eye), renderCamera.GetStereoProjectionMatrix(eye));
            }
            else
            {
                reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position, renderCamera.transform.rotation);
                reflectionCamera.worldToCameraMatrix = renderCamera.worldToCameraMatrix;
                reflectionCamera.projectionMatrix = renderCamera.projectionMatrix;
            }

            var cullEye = stereo ? (Camera.MonoOrStereoscopicEye) eye : Camera.MonoOrStereoscopicEye.Mono;
            BuildAndRender(context, renderCamera, reflectionCamera, eye, cullEye, 0f, shouldRender, false);
        }

        private void RenderCenter(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera, bool shouldRender)
        {
            GetCenterStereo(renderCamera, out var view, out var projection, out var eyeSeparation);
            ApplyViewMatrices(reflectionCamera, view, projection);
            // Each eye sees half an IPD past the shared view.
            BuildAndRender(context, renderCamera, reflectionCamera, Camera.StereoscopicEye.Left,
                Camera.MonoOrStereoscopicEye.Mono, eyeSeparation * 0.5f, shouldRender, true);
        }

        private static void ApplyViewMatrices(Camera reflectionCamera, Matrix4x4 view, Matrix4x4 projection)
        {
            reflectionCamera.worldToCameraMatrix = view;
            reflectionCamera.projectionMatrix = projection;

            // The view matrix is mirrored, so Matrix4x4.rotation returns garbage. Build it from the axes.
            var cameraToWorld = view.inverse;
            var rotation = Quaternion.LookRotation(-(Vector3) cameraToWorld.GetColumn(2), cameraToWorld.GetColumn(1));
            reflectionCamera.transform.SetPositionAndRotation(cameraToWorld.GetColumn(3), rotation);
        }

        private static void GetCenterStereo(Camera renderCamera, out Matrix4x4 view, out Matrix4x4 projection, out float eyeSeparation)
        {
            var leftView = renderCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            var rightView = renderCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

            // Same rotation, so the averaged translation is the head centre and the gap is the IPD.
            view = leftView;
            view.SetColumn(3, (leftView.GetColumn(3) + rightView.GetColumn(3)) * 0.5f);
            eyeSeparation = Vector3.Distance(leftView.GetColumn(3), rightView.GetColumn(3));

            var leftProjection = renderCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            var rightProjection = renderCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            projection = leftProjection;

            if (!TryGetFrustum(leftProjection, out var left) || !TryGetFrustum(rightProjection, out var right))
                return;

            var minX = Mathf.Min(left.Left, right.Left);
            var maxX = Mathf.Max(left.Right, right.Right);
            var minY = Mathf.Min(left.Bottom, right.Bottom);
            var maxY = Mathf.Max(left.Top, right.Top);

            // Eyes sit off to the side. They see a little past it.
            var padX = (maxX - minX) * SHARED_EYE_EXPAND * 0.5f;
            var padY = (maxY - minY) * SHARED_EYE_EXPAND * 0.5f;

            projection = Matrix4x4.Frustum(minX - padX, maxX + padX, minY - padY, maxY + padY,
                Mathf.Min(left.Near, right.Near), Mathf.Max(left.Far, right.Far));
        }

        // Frustum edges back out of a perspective matrix.
        private static bool TryGetFrustum(Matrix4x4 projection, out Frustum frustum)
        {
            frustum = default;
            if (Mathf.Abs(projection.m00) < 1e-6f || Mathf.Abs(projection.m11) < 1e-6f)
                return false;

            if (Mathf.Abs(projection.m22 - 1f) < 1e-6f || Mathf.Abs(projection.m22 + 1f) < 1e-6f)
                return false;

            frustum.Near = projection.m23 / (projection.m22 - 1f);
            frustum.Far = projection.m23 / (projection.m22 + 1f);
            if (frustum.Near <= 0f || frustum.Far <= frustum.Near)
                return false;

            frustum.Left = frustum.Near * (projection.m02 - 1f) / projection.m00;
            frustum.Right = frustum.Near * (projection.m02 + 1f) / projection.m00;
            frustum.Bottom = frustum.Near * (projection.m12 - 1f) / projection.m11;
            frustum.Top = frustum.Near * (projection.m12 + 1f) / projection.m11;
            return true;
        }

        private void BuildAndRender(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera,
            Camera.StereoscopicEye eye, Camera.MonoOrStereoscopicEye cullEye, float viewMargin, bool shouldRender, bool bothEyes)
        {
            _planner.Build(renderCamera, reflectionCamera, _Surfaces, _Profile, cullEye, viewMargin);
            RenderPlan(context, renderCamera, reflectionCamera, eye, shouldRender, bothEyes);
        }

        private void RenderPlan(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera,
            Camera.StereoscopicEye eye, bool shouldRender, bool bothEyes)
        {
            var steps = _planner.Steps;
            var recursions = _Profile.Recursions;
            var oldPixelLights = QualitySettings.pixelLightCount;
            var oldLodBias = QualitySettings.lodBias;
            var renderScale = UniversalRenderPipeline.asset != null ? UniversalRenderPipeline.asset.renderScale : 1f;
            var scaleOverridden = !Mathf.Approximately(renderScale, 1f);
            var lodBiased = !Mathf.Approximately(_Profile.LodBias, 1f);
            _finalBindings.Clear();

            try
            {
                if (_Profile.DisablePixelLights)
                    QualitySettings.pixelLightCount = 0;

                // Culling reads this, so it has to land before the reflection camera is submitted.
                if (lodBiased)
                    QualitySettings.lodBias = oldLodBias * _Profile.LodBias;

                if (scaleOverridden)
                    ApplyRenderScale(1f);

                foreach (var step in steps)
                {
                    var surface = step.Surface;
                    if (surface == null)
                        continue;

                    var flipY = ShouldFlip(renderCamera, step.Depth);
                    surface.SetEye(eye);
                    surface.SetFlipY(flipY);
                    surface.SetBackground(_Profile.EnvironmentBackground);

                    if (step.BeyondRange || step.Depth >= recursions + 1)
                    {
                        surface.ShowFallback();
                        KeepShallowest(surface, new ReflectionBinding {Depth = step.Depth, FlipY = flipY, Fallback = true});
                        continue;
                    }

                    var pooled = _resources.Acquire(renderCamera, eye, _Profile);

                    var blend = surface.CalculateBlend(step.Depth, recursions, step.Distance);
                    BindReflection(surface, eye, bothEyes, pooled.Texture, step.ViewProjection, shouldRender);
                    surface.SetBlend(blend);

                    KeepShallowest(surface, new ReflectionBinding
                    {
                        Depth = step.Depth,
                        Texture = pooled.Texture,
                        ViewProjection = step.ViewProjection,
                        Blend = blend,
                        FlipY = flipY
                    });

                    if (shouldRender)
                        DrawStep(context, renderCamera, reflectionCamera, step, pooled.Texture);
                }

                ApplyFinalBindings(eye, bothEyes, shouldRender);
            }
            finally
            {
                GL.invertCulling = false;
                QualitySettings.pixelLightCount = oldPixelLights;
                if (lodBiased)
                    QualitySettings.lodBias = oldLodBias;

                if (scaleOverridden)
                    ApplyRenderScale(renderScale);

                reflectionCamera.targetTexture = null;
                reflectionCamera.ResetCullingMatrix();
                _resources.ReleaseAll();
            }
        }

        private void KeepShallowest(MirrorSurface surface, ReflectionBinding binding)
        {
            if (_finalBindings.TryGetValue(surface, out var existing) && existing.Depth <= binding.Depth)
                return;

            _finalBindings[surface] = binding;
        }

        // A mirror can sit at several depths. The viewer sees the shallowest, so it goes on last.
        private void ApplyFinalBindings(Camera.StereoscopicEye eye, bool bothEyes, bool writeMatrix)
        {
            foreach (var pair in _finalBindings)
            {
                var surface = pair.Key;
                var binding = pair.Value;
                surface.SetFlipY(binding.FlipY);

                if (binding.Fallback || binding.Texture == null)
                {
                    surface.ShowFallback();
                    continue;
                }

                BindReflection(surface, eye, bothEyes, binding.Texture, binding.ViewProjection, writeMatrix);
                surface.SetBlend(binding.Blend);
            }
        }

        // Skipped frame keeps the old matrix, so the old texture still fits.
        private static void BindReflection(MirrorSurface surface, Camera.StereoscopicEye eye, bool bothEyes,
            RenderTexture texture, Matrix4x4 viewProjection, bool writeMatrix)
        {
            surface.SetReflectionTexture(eye, texture);
            if (writeMatrix)
                surface.SetReflectionMatrix(eye, viewProjection);

            if (!bothEyes)
                return;

            var other = eye == Camera.StereoscopicEye.Left ? Camera.StereoscopicEye.Right : Camera.StereoscopicEye.Left;
            surface.SetReflectionTexture(other, texture);
            if (writeMatrix)
                surface.SetReflectionMatrix(other, viewProjection);
        }

        private void DrawStep(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera,
            ReflectionStep step, RenderTexture target)
        {
            reflectionCamera.targetTexture = target;
            reflectionCamera.transform.position = step.CameraPosition;
            reflectionCamera.worldToCameraMatrix = step.WorldToCameraMatrix;
            reflectionCamera.projectionMatrix = step.ProjectionMatrix;
            reflectionCamera.cullingMatrix = step.CullingMatrix;

            GL.invertCulling = step.InvertCulling;

#if UNITY_EDITOR
            // ShouldServe already turned the scene view away, so this only catches a toggle mid frame.
            if (renderCamera.cameraType == CameraType.SceneView && !IncludeSceneView)
            {
                GL.invertCulling = false;
                return;
            }
#endif

            EnqueueBackgroundFade(reflectionCamera, step.CullingMatrix);

#pragma warning disable CS0618 // RenderSingleCamera is obsolete. SubmitRenderRequest recurses here.
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
#pragma warning restore CS0618

            GL.invertCulling = false;
        }

        // URP drops the queue after each render, so this goes in every time.
        private void EnqueueBackgroundFade(Camera reflectionCamera, Matrix4x4 cullingMatrix)
        {
            var fade = _Profile.BackgroundFade;
            if (fade <= 0f || _Profile.BackgroundFadeShader == null)
                return;

            if (_fadePass == null)
            {
                _fadeMaterial = CoreUtils.CreateEngineMaterial(_Profile.BackgroundFadeShader);
                _fadePass = new MirrorBackgroundFadePass(_fadeMaterial);
            }

            // URP only honours the camera's renderer for game cameras. The rest get the default one.
            var isGame = reflectionCamera.cameraType is CameraType.Game or CameraType.VR;
            var urpRenderer = isGame
                ? reflectionCamera.GetUniversalAdditionalCameraData().scriptableRenderer
                : UniversalRenderPipeline.asset != null ? UniversalRenderPipeline.asset.scriptableRenderer : null;
            if (urpRenderer == null)
                return;

            _fadePass.SetFadeLength(fade);
            _fadePass.SetCullingMatrix(cullingMatrix);
            urpRenderer.EnqueuePass(_fadePass);
        }

        // URP scales target texture cameras too and pushes the scale to XR. Set one, then restore it.
        private static void ApplyRenderScale(float scale)
        {
            if (UniversalRenderPipeline.asset == null)
                return;

            UniversalRenderPipeline.asset.renderScale = scale;
            UnityEngine.Experimental.Rendering.XRSystem.SetRenderScale(scale);
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
            // The virtual eye sits behind the glass, so baked occlusion can be wrong.
            reflectionCamera.useOcclusionCulling = _Profile.OcclusionCulling;

            // Culling reads this. Unclamped, the beam reaches the camera far plane.
            if (_Profile.FarClip > 0f)
                reflectionCamera.farClipPlane = Mathf.Min(renderCamera.farClipPlane, _Profile.FarClip);

            // Transparent clear marks what the reflection did not draw.
            if (_Profile.EnvironmentBackground)
            {
                reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
                reflectionCamera.backgroundColor = Color.clear;
            }
            else if (_Profile.OverrideClearFlags)
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

            if (cam.cameraType is CameraType.Reflection or CameraType.Preview)
                return false;

#if UNITY_EDITOR
            // Preview cameras say SceneView too.
            if (cam.cameraType == CameraType.SceneView)
                return IncludeSceneView && IsOpenSceneView(cam);
#endif

            if (!cam.CompareTag("MainCamera"))
                return false;

            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            return data == null || data.renderType != CameraRenderType.Overlay;
        }

#if UNITY_EDITOR
        private static bool IsOpenSceneView(Camera camera)
        {
            foreach (UnityEditor.SceneView view in UnityEditor.SceneView.sceneViews)
            {
                if (view != null && view.camera == camera)
                    return true;
            }

            return false;
        }
#endif

        private static MirrorStereoMode GetStereoMode(Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView || !XRSettings.enabled
                || XRSettings.eyeTextureWidth <= 0 || !camera.stereoEnabled)
                return MirrorStereoMode.None;

            return XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass
                ? MirrorStereoMode.MultiPass
                : MirrorStereoMode.SinglePass;
        }

        private void PruneCameraKeys()
        {
            _deadSkipKeys.Clear();
            foreach (var pair in _frameSkip)
            {
                if (pair.Key == null)
                    _deadSkipKeys.Add(pair.Key);
            }

            foreach (var key in _deadSkipKeys)
                _frameSkip.Remove(key);

            _deadSkipKeys.Clear();
            foreach (var pair in _sharedFrame)
            {
                if (pair.Key == null)
                    _deadSkipKeys.Add(pair.Key);
            }

            foreach (var key in _deadSkipKeys)
                _sharedFrame.Remove(key);
        }

        // False when the frame is taken. Multi pass renders once.
        private bool ClaimSharedFrame(Camera cam)
        {
            if (_sharedFrame.TryGetValue(cam, out var frame) && frame == Time.frameCount)
                return false;

            _sharedFrame[cam] = Time.frameCount;
            return true;
        }

        private bool ConsumeFrameSkip(Camera cam)
        {
            var needed = _Profile.UpdateInterval;
            if (needed <= 0)
                return true;

            if (!_frameSkip.TryGetValue(cam, out var state))
                state = new FrameSkipState {Frame = -1, Remaining = 0, Render = true};

            // Multi pass asks once per eye. Both eyes need the same answer.
            if (state.Frame == Time.frameCount)
                return state.Render;

            state.Frame = Time.frameCount;
            state.Render = state.Remaining <= 0;
            state.Remaining = state.Render ? needed : state.Remaining - 1;
            _frameSkip[cam] = state;
            return state.Render;
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
