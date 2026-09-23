using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace EDIVE.Rendering.Mirrors
{
    public enum MirrorResolutionMode
    {
        Fixed,
        ScreenFraction
    }

    public enum MirrorAntiAliasing
    {
        None = 1,
        Low = 2,
        Medium = 4,
        High = 8
    }

    public enum MirrorDepthBits
    {
        Low = 16,
        High = 24
    }

    public enum MirrorStereoEyeMode
    {
        SharedCenter,
        PerEye
    }

    public class MirrorProfile : ScriptableObject
    {
        [SerializeField]
        private MirrorResolutionMode _ResolutionMode = MirrorResolutionMode.ScreenFraction;

        [SerializeField]
        [ShowIf(nameof(_ResolutionMode), MirrorResolutionMode.Fixed)]
        private Vector2Int _FixedResolution = new(512, 512);

        [SerializeField]
        [Tooltip("Share of the eye, before the mirror size is taken into account.")]
        [ShowIf(nameof(_ResolutionMode), MirrorResolutionMode.ScreenFraction)]
        [Range(0.05f, 1f)]
        private float _ScreenFraction = 1f;

        [SerializeField]
        [Tooltip("HDR kills banding. Costs more.")]
        private RenderTextureFormat _Format = RenderTextureFormat.Default;

        [SerializeField]
        [Tooltip("16 is cheaper. Try it on mobile VR, watch for z fighting.")]
        private MirrorDepthBits _DepthBits = MirrorDepthBits.High;

        [SerializeField]
        private MirrorAntiAliasing _AntiAliasing = MirrorAntiAliasing.Low;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Leave empty to never flip.")]
        private MirrorFlipRules _FlipRules;

        [SerializeField]
        [Tooltip("Point is sharp. Bilinear is smooth.")]
        private FilterMode _FilterMode = FilterMode.Bilinear;

        [SerializeField]
        [Tooltip("What happens past the texture edge. Blur and refraction go there.")]
        private TextureWrapMode _WrapMode = TextureWrapMode.Mirror;

        [SerializeField]
        [Tooltip("Made up front. Avoids a hitch on first use.")]
        [MinValue(0)]
        private int _PreallocatedTextures = 1;

        [Title("Cost")]
        [SerializeField]
        [Tooltip("Shared Center halves the VR cost. Per Eye gives the reflection real depth.")]
        private MirrorStereoEyeMode _StereoEyeMode = MirrorStereoEyeMode.SharedCenter;

        [PropertySpace]
        [SerializeField]
        [Tooltip("How many times a mirror can show another mirror.")]
        [PropertyRange(1, 8)]
        private int _Recursions = 1;

        [SerializeField]
        [Tooltip("Frames to skip between updates. The reflection stays glued, only parallax lags.")]
        [MinValue(0)]
        private int _UpdateInterval;

        [SerializeField]
        [Tooltip("Far plane for the reflection only. 0 inherits the camera. The single biggest cost lever.")]
        [MinValue(0f)]
        private float _FarClip;

        [SerializeField]
        [Tooltip("Cull the reflection against baked occlusion. The virtual eye sits behind the glass, so check the mirror still looks right.")]
        private bool _OcclusionCulling;

        [SerializeField]
        [Tooltip("Scales the quality level's LOD bias for the reflection. Lower drops to cheaper meshes sooner. Only bites if the scene has LOD groups.")]
        [PropertyRange(0.05f, 1f)]
        private float _LodBias = 1f;

        [PropertySpace]
        [SerializeField]
        private LayerMask _RenderLayers = ~0;

        [SerializeField]
        [Tooltip("Renderer index in the URP asset.")]
        [MinValue(0)]
        private int _RendererIndex;

        [SerializeField]
        private bool _RenderShadows;

        [SerializeField]
        [Tooltip("Usually wasted. The screen gets post processing anyway.")]
        private bool _RenderPostProcessing;

        [SerializeField]

        private CameraOverrideOption _OpaqueTexture = CameraOverrideOption.Off;

        [SerializeField]
        private CameraOverrideOption _DepthTexture = CameraOverrideOption.Off;

        [SerializeField]
        private bool _DisablePixelLights = true;

        [SerializeField]
        [Tooltip("Empty uses the camera skybox.")]
        private Material _CustomSkybox;

        [SerializeField]
        private bool _OverrideClearFlags;

        [SerializeField]
        [ShowIf(nameof(_OverrideClearFlags))]
        private CameraClearFlags _ClearFlags = CameraClearFlags.Color;

        [SerializeField]
        [ShowIf(nameof(_OverrideClearFlags))]
        private Color _ClearColor = Color.black;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Extra room around the culling frustum. Stops popping at the edges.")]
        [Range(0f, 0.25f)]
        private float _FrustumPadding = 0.02f;

        [SerializeField]
        [Tooltip("Pulls the culling near plane in front of the mirror. Stops things touching the glass vanishing.")]
        [Range(0f, 0.5f)]
        private float _CullingNearOffset = 0.05f;

        public MirrorResolutionMode ResolutionMode => _ResolutionMode;
        public RenderTextureFormat Format => _Format;
        public int DepthBits => (int) _DepthBits;
        public MirrorAntiAliasing AntiAliasing => _AntiAliasing;
        public int PreallocatedTextures => Mathf.Max(0, _PreallocatedTextures);
        public MirrorStereoEyeMode StereoEyeMode => _StereoEyeMode;
        public int Recursions => Mathf.Clamp(_Recursions, 1, 8);
        public int UpdateInterval => Mathf.Max(0, _UpdateInterval);
        public float FarClip => Mathf.Max(0f, _FarClip);
        public bool OcclusionCulling => _OcclusionCulling;
        // Zero means a profile saved before this field existed. Treat it as no change, not as the floor.
        public float LodBias => _LodBias <= 0f ? 1f : Mathf.Clamp(_LodBias, 0.05f, 1f);
        public LayerMask RenderLayers => _RenderLayers;
        public int RendererIndex => Mathf.Max(0, _RendererIndex);
        public bool RenderShadows => _RenderShadows;
        public bool RenderPostProcessing => _RenderPostProcessing;
        public CameraOverrideOption OpaqueTexture => _OpaqueTexture;
        public CameraOverrideOption DepthTexture => _DepthTexture;
        public bool DisablePixelLights => _DisablePixelLights;
        public Material CustomSkybox => _CustomSkybox;
        public bool OverrideClearFlags => _OverrideClearFlags;
        public CameraClearFlags ClearFlags => _ClearFlags;
        public Color ClearColor => _ClearColor;
        public float FrustumPadding => _FrustumPadding;
        public float CullingNearOffset => _CullingNearOffset;
        public MirrorFlipRules FlipRules => _FlipRules;
        public FilterMode FilterMode => _FilterMode;
        public TextureWrapMode WrapMode => _WrapMode;

        public bool SharedCenterEye => _StereoEyeMode == MirrorStereoEyeMode.SharedCenter;

        public event Action Changed;

        public Vector2Int GetResolution(Camera camera)
        {
            GetViewSize(camera, out var width, out var height);

            var size = _ResolutionMode == MirrorResolutionMode.Fixed
                ? _FixedResolution
                : new Vector2Int(Mathf.RoundToInt(width * _ScreenFraction), Mathf.RoundToInt(height * _ScreenFraction));

            return new Vector2Int(Mathf.Max(4, size.x), Mathf.Max(4, size.y));
        }

        // camera.pixelWidth can report both eyes at once in a VR build.
        private static void GetViewSize(Camera camera, out int width, out int height)
        {
            if (camera != null && camera.stereoEnabled && camera.cameraType != CameraType.SceneView
                && XRSettings.enabled && XRSettings.eyeTextureWidth > 0)
            {
                width = XRSettings.eyeTextureWidth;
                height = XRSettings.eyeTextureHeight;
                return;
            }

            width = camera != null ? camera.pixelWidth : Screen.width;
            height = camera != null ? camera.pixelHeight : Screen.height;
        }

        public RenderTextureDescriptor GetDescriptor(Camera camera, bool allowMsaa)
        {
            return GetDescriptor(GetResolution(camera), allowMsaa);
        }

        public RenderTextureDescriptor GetDescriptor(Vector2Int size, bool allowMsaa)
        {
            return new RenderTextureDescriptor(size.x, size.y, _Format, DepthBits)
            {
                vrUsage = VRTextureUsage.None,
                useMipMap = false,
                msaaSamples = allowMsaa ? (int) _AntiAliasing : 1
            };
        }

        // OnValidate is editor only.
        public void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private void OnValidate()
        {
            Changed?.Invoke();
        }
    }
}
