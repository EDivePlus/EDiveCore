using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Rendering.Mirrors
{
    public enum MirrorEnvironment
    {
        Color,
        ReflectionProbe,
        DepthProbe
    }

    [ExecuteAlways]
    public class MirrorSurface : MonoBehaviour
    {
        private static readonly int MIRROR_TEX_LEFT = Shader.PropertyToID("_MirrorTexLeft");
        private static readonly int MIRROR_TEX_RIGHT = Shader.PropertyToID("_MirrorTexRight");
        private static readonly int MIRROR_BLEND = Shader.PropertyToID("_MirrorBlend");
        private static readonly int MIRROR_BACKGROUND = Shader.PropertyToID("_MirrorBackground");
        private static readonly int MIRROR_EYE = Shader.PropertyToID("_MirrorEye");
        private static readonly int MIRROR_FLIP_Y = Shader.PropertyToID("_MirrorFlipY");
        private static readonly int MIRROR_VP_LEFT = Shader.PropertyToID("_MirrorVpLeft");
        private static readonly int MIRROR_VP_RIGHT = Shader.PropertyToID("_MirrorVpRight");

        private static readonly int FALLBACK_CUBEMAP = Shader.PropertyToID("_FallbackCubemap");
        private static readonly int FALLBACK_CUBEMAP_HDR = Shader.PropertyToID("_FallbackCubemapHDR");
        private static readonly int FALLBACK_PROBE_POS = Shader.PropertyToID("_FallbackProbePos");
        private static readonly int FALLBACK_BOX_MIN = Shader.PropertyToID("_FallbackBoxMin");
        private static readonly int FALLBACK_BOX_MAX = Shader.PropertyToID("_FallbackBoxMax");
        private static readonly int DEPTH_PROBE = Shader.PropertyToID("_DepthProbe");
        private static readonly int DEPTH_PROBE_POS = Shader.PropertyToID("_DepthProbePos");
        private static readonly int DEPTH_PROBE_DISTANCE = Shader.PropertyToID("_DepthProbeDistance");
        private static readonly int DEPTH_PROBE_STEPS = Shader.PropertyToID("_DepthProbeSteps");
        private static readonly int ENVIRONMENT = Shader.PropertyToID("_Environment");
        private static readonly int ENVIRONMENT_COLOR = Shader.PropertyToID("_FallbackEnvColor");
        private static readonly int BOX_PROJECTION = Shader.PropertyToID("_BoxProjection");

        [SerializeField]
        [Required]
        private MeshRenderer _MeshRenderer;

        [SerializeField]
        [Tooltip("Which material slot is the mirror.")]
        [MinValue(0)]
        private int _MaterialIndex;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Defaults to this transform. -Z faces out.")]
        private Transform _ForwardTransform;
        
        [SerializeField]
        [Tooltip("Past this the mirror shows the fallback.")]
        [MinValue(0f)]
        private float _RenderDistance = 5f;
        
        [PropertySpace]
        [SerializeField]
        [Tooltip("Moves the clip plane. Hides edge seams.")]
        private float _ClippingPlaneOffset;

        [SerializeField]
        [Tooltip("Metres before the limit where fading starts.")]
        [MinValue(0f)]
        private float _FadeLength = 1f;

        [SerializeField]
        [Tooltip("Strongest reflection when fully faded in.")]
        [Range(0f, 1f)]
        private float _BlendStrength = 1f;

        [SerializeField]
        [Tooltip("Darken by recursion depth instead of by distance.")]
        private bool _UseDepthFalloff = true;

        [SerializeField]
        [ShowIf(nameof(_UseDepthFalloff))]
        private AnimationCurve _DepthFalloffCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [PropertySpace]
        [SerializeField]
        [Tooltip("Shown past the render distance.")]
        private MirrorEnvironment _Environment = MirrorEnvironment.Color;

        [SerializeField]
        [ShowIf(nameof(_Environment), MirrorEnvironment.Color)]
        private Color _EnvironmentColor = Color.black;

        [SerializeField]
        [ShowIf(nameof(_Environment), MirrorEnvironment.ReflectionProbe)]
        [Tooltip("Empty uses the probe Unity picks.")]
        private ReflectionProbe _FallbackProbe;

        [SerializeField]
        [ShowIf(nameof(_Environment), MirrorEnvironment.ReflectionProbe)]
        [Tooltip("Fit the probe to its box.")]
        private bool _BoxProjection = true;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe", VisibleIf = nameof(UsesDepthProbe), Order = 10)]
        [ReadOnly]
        private Cubemap _DepthProbe;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        [Tooltip("Where reflected rays stop.")]
        [ReadOnly]
        private Cubemap _DepthProbeDistance;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        [Tooltip("From the mirror centre. Z points out.")]
        private Vector3 _DepthProbeOffset = new(0f, 0f, 0.1f);

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        private LayerMask _DepthProbeCullingMask = ~0;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        [ValueDropdown(nameof(DEPTH_PROBE_RESOLUTIONS))]
        [FormerlySerializedAs("_DepthProbeResolution")]
        private int _DepthProbeColorResolution = 512;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        [Tooltip("Only sharpens outlines. Lower is faster.")]
        [ValueDropdown(nameof(DEPTH_PROBE_RESOLUTIONS))]
        private int _DepthProbeDistanceResolution = 256;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        [Tooltip("Anything further reads as sky.")]
        [MinValue(1f)]
        private float _DepthProbeRange = 60f;

        [SerializeField]
        [EnhancedBoxGroup("Depth Probe")]
        [Tooltip("Per pixel. More catches thin objects, fewer is faster.")]
        [Range(4, 32)]
        private int _DepthProbeSteps = 16;

        [SerializeField]
        [HideInInspector]
        private Vector4 _DepthProbeBakedPos;
        
        private static readonly int[] DEPTH_PROBE_RESOLUTIONS = { 64, 128, 256, 512, 1024 };

        [PropertyOrder(30)]
        [PropertySpace]
        [SerializeField]
        [Tooltip("Surfaces in the same plane that reuse this reflection.")]
        private List<MirrorSurface> _LinkedSurfaces = new();
        
        [PropertyOrder(30)]
        [SerializeField]
        [Tooltip("Boxes this mirror may see into. Empty uses only the profile far clip.")]
        private List<MirrorVisibilityVolume> _VisibilityVolumes = new();

        
        private const string INSTANCE_SUFFIX = " (mirror instance)";

        private static readonly Plane[] FRUSTUM_PLANES = new Plane[6];

        private static readonly Rect FULL_VIEWPORT = new(0, 0, 1, 1);

        private readonly Vector3[] _frustumCorners = new Vector3[4];

        [SerializeField]
        [HideInInspector]
        private Material _SourceMaterial;

        private Material _materialInstance;
        private MeshFilter _meshFilter;
        private int _enabledFrame;
        private bool _valid;

        public Transform ForwardTransform => _ForwardTransform != null ? _ForwardTransform : transform;
        public float RenderDistance => _RenderDistance;
        public bool UseDepthFalloff => _UseDepthFalloff;
        public float ClippingPlaneOffset => _ClippingPlaneOffset;
        public IReadOnlyList<MirrorSurface> LinkedSurfaces => _LinkedSurfaces;
        public Material MaterialInstance => _materialInstance;

        private void OnEnable()
        {
            _valid = Initialize();
            _enabledFrame = Time.frameCount;

            if (_valid)
                ApplyEnvironment();
        }

        private void OnDisable()
        {
            RestoreSourceMaterial();
            _valid = false;
        }
        
        [PropertySpace]
        [Button]
        public void Rebuild()
        {
            RestoreSourceMaterial();
            _valid = false;

            if (!isActiveAndEnabled)
                return;

            _valid = Initialize();
            _enabledFrame = Time.frameCount;

            if (_valid)
                ApplyEnvironment();
        }

        private bool Initialize()
        {
            if (_MeshRenderer == null)
            {
                Debug.LogError($"[Mirrors] {name} has no MeshRenderer.", this);
                return false;
            }

            _meshFilter = _MeshRenderer.GetComponent<MeshFilter>();

            var shared = _MeshRenderer.sharedMaterials;
            if (_MaterialIndex >= shared.Length)
            {
                Debug.LogError($"[Mirrors] {name} material index {_MaterialIndex} is out of range (renderer has {shared.Length}).", this);
                return false;
            }

            // Skip our own instances. Stops nesting on reload.
            var slot = shared[_MaterialIndex];
            if (slot != null && slot != _materialInstance && !slot.name.EndsWith(INSTANCE_SUFFIX))
                _SourceMaterial = slot;

            if (_SourceMaterial == null)
            {
                Debug.LogError($"[Mirrors] {name} has no material in slot {_MaterialIndex}.", this);
                return false;
            }

            // Own copy, so mirrors sharing a material get their own reflection.
            _materialInstance = new Material(_SourceMaterial) { name = _SourceMaterial.name + INSTANCE_SUFFIX };
            SetMaterialAt(_MaterialIndex, _materialInstance);
            return true;
        }

        private void RestoreSourceMaterial()
        {
            if (_MeshRenderer != null && _SourceMaterial != null)
                SetMaterialAt(_MaterialIndex, _SourceMaterial);

            if (_materialInstance == null)
                return;

            if (Application.isPlaying)
                Destroy(_materialInstance);
            else
                DestroyImmediate(_materialInstance);

            _materialInstance = null;
        }

        private void SetMaterialAt(int index, Material material)
        {
            var shared = _MeshRenderer.sharedMaterials;
            if (index < 0 || index >= shared.Length)
                return;

            shared[index] = material;
            _MeshRenderer.sharedMaterials = shared;
        }

        // Read only. Safe to call from the recursion.
        public bool IsVisibleFrom(Camera cam, bool ignoreDistance)
        {
            if (!_valid || !enabled || !gameObject.activeInHierarchy || _MeshRenderer == null)
                return false;

            // Occlusion culled, so skip the whole reflection. Lags a frame on the way back in.
            if (Time.frameCount - _enabledFrame > 1 && !_MeshRenderer.isVisible)
                return false;

            var forward = -ForwardTransform.forward;
            if (Vector3.Dot(forward, cam.transform.position - ForwardTransform.position) < 0)
                return false;

            if (!ignoreDistance && Vector3.Distance(ClosestPoint(cam.transform.position), cam.transform.position) > _RenderDistance)
                return false;

            GeometryUtility.CalculateFrustumPlanes(cam, FRUSTUM_PLANES);
            return GeometryUtility.TestPlanesAABB(FRUSTUM_PLANES, _MeshRenderer.bounds);
        }

        public Vector3 ClosestPoint(Vector3 position)
        {
            return _MeshRenderer != null ? _MeshRenderer.bounds.ClosestPoint(position) : transform.position;
        }

        public void SetReflectionTexture(Camera.StereoscopicEye eye, RenderTexture texture)
        {
            if (_materialInstance == null || texture == null)
                return;

            _materialInstance.SetTexture(eye == Camera.StereoscopicEye.Left ? MIRROR_TEX_LEFT : MIRROR_TEX_RIGHT, texture);

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetReflectionTexture(eye, texture);
            }
        }

        // Shader finds its texel from a world position, not the screen.
        public void SetReflectionMatrix(Camera.StereoscopicEye eye, Matrix4x4 viewProjection)
        {
            if (_materialInstance != null)
                _materialInstance.SetMatrix(eye == Camera.StereoscopicEye.Left ? MIRROR_VP_LEFT : MIRROR_VP_RIGHT, viewProjection);

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetReflectionMatrix(eye, viewProjection);
            }
        }

        public void SetEye(Camera.StereoscopicEye eye)
        {
            SetForceEye(eye == Camera.StereoscopicEye.Left ? 0 : 1);
        }

        public void SetForceEye(int value)
        {
            if (_materialInstance != null)
                _materialInstance.SetFloat(MIRROR_EYE, value);

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetForceEye(value);
            }
        }

        public void SetFlipY(bool flip)
        {
            if (_materialInstance != null)
                _materialInstance.SetFloat(MIRROR_FLIP_Y, flip ? 1f : 0f);

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetFlipY(flip);
            }
        }

        // 0 shows the fallback, 1 shows the live reflection.
        public void SetBlend(float blend)
        {
            if (_materialInstance != null)
                _materialInstance.SetFloat(MIRROR_BLEND, Mathf.Clamp01(blend));

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetBlend(blend);
            }
        }

        // On, the environment shows wherever the reflection drew nothing.
        public void SetBackground(bool background)
        {
            if (_materialInstance != null)
                _materialInstance.SetFloat(MIRROR_BACKGROUND, background ? 1f : 0f);

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetBackground(background);
            }
        }

        public void ShowFallback()
        {
            SetBlend(0f);
        }

        public float CalculateBlend(int depth, int recursions, float distance)
        {
            if (_UseDepthFalloff && depth > 1)
            {
                var t = 1f - (depth - 1f) / Mathf.Max(1, recursions);
                return Mathf.Clamp01(_DepthFalloffCurve.Evaluate(t)) * _BlendStrength;
            }

            var fadeBand = Mathf.Min(_FadeLength, _RenderDistance);
            if (fadeBand <= Mathf.Epsilon)
                return distance > _RenderDistance ? 0f : _BlendStrength;

            var faded = distance - (_RenderDistance - fadeBand);
            return Mathf.Clamp01(1f - faded / fadeBand) * _BlendStrength;
        }

        private bool UsesDepthProbe() => _Environment == MirrorEnvironment.DepthProbe;

        public void ApplyEnvironment()
        {
            if (_materialInstance == null)
                return;

            _materialInstance.SetFloat(ENVIRONMENT, (float) _Environment);
            _materialInstance.SetColor(ENVIRONMENT_COLOR, _EnvironmentColor);
            _materialInstance.SetFloat(BOX_PROJECTION, _BoxProjection ? 1f : 0f);
            _materialInstance.SetFloat(DEPTH_PROBE_STEPS, _DepthProbeSteps);
            _materialInstance.SetTexture(DEPTH_PROBE, _DepthProbe);
            _materialInstance.SetTexture(DEPTH_PROBE_DISTANCE, _DepthProbeDistance);
            _materialInstance.SetVector(DEPTH_PROBE_POS, _DepthProbeBakedPos);

            // Zero W tells the shader to use the probe Unity picked.
            if (_FallbackProbe == null || _FallbackProbe.texture == null)
            {
                _materialInstance.SetVector(FALLBACK_PROBE_POS, Vector4.zero);
                return;
            }

            var probeTransform = _FallbackProbe.transform;
            var center = (Vector4) (probeTransform.position + _FallbackProbe.center);
            var half = (Vector4) _FallbackProbe.size * 0.5f;

            var min = center - half;
            var max = center + half;
            center.w = 1f;
            min.w = 1f;
            max.w = 1f;

            _materialInstance.SetTexture(FALLBACK_CUBEMAP, _FallbackProbe.texture);
            _materialInstance.SetVector(FALLBACK_CUBEMAP_HDR, _FallbackProbe.textureHDRDecodeValues);
            _materialInstance.SetVector(FALLBACK_PROBE_POS, center);
            _materialInstance.SetVector(FALLBACK_BOX_MIN, min);
            _materialInstance.SetVector(FALLBACK_BOX_MAX, max);
        }

        // Frustum that just covers the mirror, for culling. viewMargin widens it for a shared centre view.
        public bool TryGetCullingMatrices(Camera reflectionCamera, MirrorProfile profile,
            Camera.MonoOrStereoscopicEye eye, float viewMargin, out Matrix4x4 view, out Matrix4x4 projection)
        {
            view = Matrix4x4.identity;
            projection = Matrix4x4.identity;

            var mirror = ForwardTransform;
            var right = mirror.right;
            var up = mirror.up;
            var normal = -mirror.forward;
            var eyePos = reflectionCamera.transform.position;

            var planeDistance = Vector3.Dot(eyePos - mirror.position, normal);

            // Reflection cameras sit behind the mirror. Flip the axes, right too, or the frustum turns inside out.
            if (planeDistance < 0f)
            {
                normal = -normal;
                right = -right;
                planeDistance = -planeDistance;
            }

            if (planeDistance < 1e-4f)
                return false;

            if (!TryGetMirrorRect(right, up, eyePos, out var clipped))
                return false;

            // Cut it down to what the camera can see.
            if (TryGetViewRect(reflectionCamera, eye, right, up, normal, planeDistance, out var viewRect))
            {
                viewRect = Rect.MinMaxRect(viewRect.xMin - viewMargin, viewRect.yMin - viewMargin,
                    viewRect.xMax + viewMargin, viewRect.yMax + viewMargin);

                if (!Intersect(clipped, viewRect, out clipped))
                    return false;
            }

            // Author-placed boxes. Trims the beam sideways and caps how far it reaches.
            var volumeFar = float.PositiveInfinity;
            if (!ClampToVolumes(right, up, normal, eyePos, planeDistance, ref clipped, ref volumeFar))
                return false;

            var padX = clipped.width * profile.FrustumPadding;
            var padY = clipped.height * profile.FrustumPadding;
            clipped = Rect.MinMaxRect(clipped.xMin - padX, clipped.yMin - padY, clipped.xMax + padX, clipped.yMax + padY);

            // Near plane goes in front of the mirror. Stuff on the glass still draws.
            var near = Mathf.Max(planeDistance - profile.CullingNearOffset, planeDistance * 0.01f);
            var far = Mathf.Min(reflectionCamera.farClipPlane, volumeFar);
            if (near >= far)
                return false;

            // Rect was measured at the mirror. The frustum wants it at the near plane.
            var scale = near / planeDistance;

            projection = Matrix4x4.Frustum(clipped.xMin * scale, clipped.xMax * scale,
                clipped.yMin * scale, clipped.yMax * scale, near, far);

            // View from the eye, on the mirror axes. Row 2 is -forward.
            view = Matrix4x4.identity;
            view.SetRow(0, new Vector4(right.x, right.y, right.z, -Vector3.Dot(right, eyePos)));
            view.SetRow(1, new Vector4(up.x, up.y, up.z, -Vector3.Dot(up, eyePos)));
            view.SetRow(2, new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, eyePos)));

            return true;
        }

        // Boxes flattened onto the mirror plane. Their union caps the beam width, their deepest corner its far plane.
        private bool ClampToVolumes(Vector3 right, Vector3 up, Vector3 normal, Vector3 eyePos,
            float planeDistance, ref Rect clipped, ref float far)
        {
            if (_VisibilityVolumes == null || _VisibilityVolumes.Count == 0)
                return true;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var maxDepth = 0f;
            var anyInFront = false;
            var straddlesEye = false;

            foreach (var volume in _VisibilityVolumes)
            {
                if (volume == null)
                    continue;

                foreach (var corner in volume.GetWorldCorners())
                {
                    var offset = corner - eyePos;

                    // Row 2 of the view matrix is the normal, so depth in front of the eye is the negated dot.
                    var depth = -Vector3.Dot(offset, normal);
                    if (depth <= 1e-4f)
                    {
                        straddlesEye = true;
                        continue;
                    }

                    anyInFront = true;
                    maxDepth = Mathf.Max(maxDepth, depth);

                    var toPlane = planeDistance / depth;
                    var x = Vector3.Dot(offset, right) * toPlane;
                    var y = Vector3.Dot(offset, up) * toPlane;

                    min.x = Mathf.Min(min.x, x);
                    min.y = Mathf.Min(min.y, y);
                    max.x = Mathf.Max(max.x, x);
                    max.y = Mathf.Max(max.y, y);
                }
            }

            // Every box is behind the virtual eye. Collapse the beam instead of seeing the whole scene.
            if (!anyInFront)
            {
                var center = clipped.center;
                clipped = Rect.MinMaxRect(center.x - 1e-4f, center.y - 1e-4f, center.x + 1e-4f, center.y + 1e-4f);
                far = planeDistance * 1.001f;
                return true;
            }

            far = maxDepth;

            // A box wrapped around the eye has no silhouette to clip against. Keep the depth limit only.
            if (straddlesEye)
                return true;

            return Intersect(clipped, Rect.MinMaxRect(min.x, min.y, max.x, max.y), out clipped);
        }

        // Mirror size, flattened on the mirror axes, measured from the eye.
        private bool TryGetMirrorRect(Vector3 right, Vector3 up, Vector3 eyePos, out Rect rect)
        {
            rect = default;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            if (!ExpandByMesh(ref min, ref max, right, up, eyePos))
                return false;

            if (_LinkedSurfaces != null)
            {
                foreach (var child in _LinkedSurfaces)
                {
                    if (child != null && child != this)
                        child.ExpandByMesh(ref min, ref max, right, up, eyePos);
                }
            }

            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.width > 0f && rect.height > 0f;
        }

        // Grow min/max over this mesh. All 8 corners, so any angle works.
        private bool ExpandByMesh(ref Vector2 min, ref Vector2 max, Vector3 right, Vector3 up, Vector3 eyePos)
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null || _MeshRenderer == null)
                return false;

            var bounds = _meshFilter.sharedMesh.bounds;
            var boundsMin = bounds.min;
            var boundsMax = bounds.max;
            var localToWorld = _MeshRenderer.transform.localToWorldMatrix;

            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? boundsMin.x : boundsMax.x,
                    (i & 2) == 0 ? boundsMin.y : boundsMax.y,
                    (i & 4) == 0 ? boundsMin.z : boundsMax.z);

                var offset = localToWorld.MultiplyPoint3x4(corner) - eyePos;
                var x = Vector3.Dot(offset, right);
                var y = Vector3.Dot(offset, up);

                min.x = Mathf.Min(min.x, x);
                min.y = Mathf.Min(min.y, y);
                max.x = Mathf.Max(max.x, x);
                max.y = Mathf.Max(max.y, y);
            }

            return true;
        }

        // Camera frustum dropped on the mirror plane. False if a corner points away.
        private bool TryGetViewRect(Camera cam, Camera.MonoOrStereoscopicEye eye, Vector3 right, Vector3 up,
            Vector3 normal, float planeDistance, out Rect rect)
        {
            rect = default;
            cam.CalculateFrustumCorners(FULL_VIEWPORT, 1f, eye, _frustumCorners);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            // Matrix, not transform. A reflected camera has no valid rotation.
            var cameraToWorld = cam.cameraToWorldMatrix;

            for (var i = 0; i < 4; i++)
            {
                var corner = _frustumCorners[i];
                // Corners are transform space. The matrix wants -Z forward.
                var dir = cameraToWorld.MultiplyVector(new Vector3(corner.x, corner.y, -corner.z));

                var towards = -Vector3.Dot(dir, normal);
                if (towards <= 1e-6f)
                    return false;

                var hit = dir * (planeDistance / towards);
                var x = Vector3.Dot(hit, right);
                var y = Vector3.Dot(hit, up);

                min.x = Mathf.Min(min.x, x);
                min.y = Mathf.Min(min.y, y);
                max.x = Mathf.Max(max.x, x);
                max.y = Mathf.Max(max.y, y);
            }

            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private static bool Intersect(Rect a, Rect b, out Rect area)
        {
            area = default;
            if (!a.Overlaps(b))
                return false;

            area = Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
                Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

            return area.width > 0f && area.height > 0f;
        }

#if UNITY_EDITOR
        public Vector3 GetDepthProbeOrigin()
        {
            // Reflected rays start on the mirror, so capture close to it.
            var mirror = ForwardTransform;
            return mirror.position
                   + mirror.right * _DepthProbeOffset.x
                   + mirror.up * _DepthProbeOffset.y
                   - mirror.forward * _DepthProbeOffset.z;
        }

        private void OnDrawGizmosSelected()
        {
            if (!UsesDepthProbe())
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GetDepthProbeOrigin(), 0.1f);
        }

        [HorizontalGroup("Depth Probe/Buttons")]
        [Button("Bake")]
        public void BakeDepthProbe()
        {
            var origin = GetDepthProbeOrigin();

            var scenePath = gameObject.scene.path;
            var folder = string.IsNullOrEmpty(scenePath) ? "Assets" : System.IO.Path.ChangeExtension(scenePath, null);
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                folder = System.IO.Path.GetDirectoryName(scenePath);
            var assetPath = $"{folder}/MirrorDepthProbe-{name}";

            var hidden = new List<MeshRenderer>();
            HideForBake(this, hidden);
            foreach (var child in _LinkedSurfaces)
                HideForBake(child, hidden);

            bool baked;
            Cubemap color, distance;
            try
            {
                baked = MirrorDepthProbeBaker.Bake(origin, _DepthProbeColorResolution, _DepthProbeDistanceResolution, 0.05f, _DepthProbeRange,
                    _DepthProbeCullingMask, assetPath, out color, out distance);
            }
            finally
            {
                foreach (var meshRenderer in hidden)
                    meshRenderer.enabled = true;
            }

            if (!baked)
                return;

            var bakedPos = new Vector4(origin.x, origin.y, origin.z, _DepthProbeRange);
            SetDepthProbe(this, color, distance, bakedPos);
            foreach (var child in _LinkedSurfaces)
            {
                if (child != null && child != this)
                    SetDepthProbe(child, color, distance, bakedPos);
            }
        }
        
        [HorizontalGroup("Depth Probe/Buttons")]
        [Button("Clear")]
        [EnableIf("@_DepthProbe != null || _DepthProbeDistance != null")]
        public void ClearDepthProbe()
        {
            var assets = new[] { _DepthProbe, _DepthProbeDistance };

            SetDepthProbe(this, null, null, Vector4.zero);
            foreach (var child in _LinkedSurfaces)
            {
                if (child != null && child != this)
                    SetDepthProbe(child, null, null, Vector4.zero);
            }

            foreach (var asset in assets)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                    UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }

        private static void SetDepthProbe(MirrorSurface surface, Cubemap color, Cubemap distance, Vector4 bakedPos)
        {
            UnityEditor.Undo.RecordObject(surface, "Depth Probe");
            surface._DepthProbe = color;
            surface._DepthProbeDistance = distance;
            surface._DepthProbeBakedPos = bakedPos;
            UnityEditor.EditorUtility.SetDirty(surface);
            surface.ApplyEnvironment();
        }

        private static void HideForBake(MirrorSurface surface, List<MeshRenderer> hidden)
        {
            if (surface == null || surface._MeshRenderer == null || !surface._MeshRenderer.enabled)
                return;

            surface._MeshRenderer.enabled = false;
            hidden.Add(surface._MeshRenderer);
        }

        private void OnValidate()
        {
            if (_materialInstance != null)
                ApplyEnvironment();
        }
#endif
    }
}
