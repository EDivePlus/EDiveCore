using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    
    public class MirrorProfile : ScriptableObject
    {
        [SerializeField]
        private MirrorResolutionMode _ResolutionMode = MirrorResolutionMode.ScreenFraction;

        [SerializeField]
        [ShowIf(nameof(_ResolutionMode), MirrorResolutionMode.Fixed)]
        private Vector2Int _FixedResolution = new(512, 512);

        [SerializeField]
        [ShowIf(nameof(_ResolutionMode), MirrorResolutionMode.ScreenFraction)]
        [Range(0.05f, 1f)]
        private float _ScreenFraction = 1f;

        [SerializeField]
        [Tooltip("HDR kills banding. Costs more.")]
        private RenderTextureFormat _Format = RenderTextureFormat.Default;

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

        [PropertySpace]
        [SerializeField]
        [Tooltip("How many times a mirror can show another mirror.")]
        [PropertyRange(1, 8)]
        private int _Recursions = 1;

        [SerializeField]
        [Tooltip("Frames to skip between updates. Use 0 in VR.")]
        [MinValue(0)]
        private int _UpdateInterval;

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
        [Tooltip("Shrinks the reflection frustum to the mirror. Cheap. Keep it on.")]
        private bool _TightFrustumCulling = true;

        [SerializeField]
        [Tooltip("Extra room around the mirror. Stops popping at the edges.")]
        [ShowIf(nameof(_TightFrustumCulling))]
        [Range(0f, 0.25f)]
        private float _FrustumPadding = 0.02f;

        [SerializeField]
        [Tooltip("Moves the near plane in front of the mirror. Stops stuff on the glass vanishing.")]
        [ShowIf(nameof(_TightFrustumCulling))]
        [Range(0f, 0.5f)]
        private float _CullingNearOffset = 0.05f;


        public MirrorResolutionMode ResolutionMode => _ResolutionMode;
        public RenderTextureFormat Format => _Format;
        public MirrorAntiAliasing AntiAliasing => _AntiAliasing;
        public int PreallocatedTextures => Mathf.Max(0, _PreallocatedTextures);
        public int Recursions => Mathf.Clamp(_Recursions, 1, 8);
        public int UpdateInterval => Mathf.Max(0, _UpdateInterval);
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
        public bool TightFrustumCulling => _TightFrustumCulling;
        public float FrustumPadding => _FrustumPadding;
        public float CullingNearOffset => _CullingNearOffset;
        public MirrorFlipRules FlipRules => _FlipRules;
        public FilterMode FilterMode => _FilterMode;
        public TextureWrapMode WrapMode => _WrapMode;
        
        public event Action Changed;
        
        public Vector2Int GetResolution(Camera camera)
        {
            var width = camera != null ? camera.pixelWidth : Screen.width;
            var height = camera != null ? camera.pixelHeight : Screen.height;

            var size = _ResolutionMode == MirrorResolutionMode.Fixed
                ? _FixedResolution
                : new Vector2Int(Mathf.RoundToInt(width * _ScreenFraction), Mathf.RoundToInt(height * _ScreenFraction));

            return new Vector2Int(Mathf.Max(4, size.x), Mathf.Max(4, size.y));
        }

        public RenderTextureDescriptor GetDescriptor(Camera camera, bool allowMsaa)
        {
            var size = GetResolution(camera);
            return new RenderTextureDescriptor(size.x, size.y, _Format, 24)
            {
                vrUsage = VRTextureUsage.None,
                useMipMap = false,
                msaaSamples = allowMsaa ? (int) _AntiAliasing : 1
            };
        }

        private void OnValidate()
        {
            Changed?.Invoke();
        }
    }
}
