using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Rendering.Mirrors
{
    [ExecuteAlways]
    public class MirrorSurface : MonoBehaviour
    {
        private static readonly int MIRROR_TEX_LEFT = Shader.PropertyToID("_MirrorTexLeft");
        private static readonly int MIRROR_TEX_RIGHT = Shader.PropertyToID("_MirrorTexRight");
        private static readonly int MIRROR_BLEND = Shader.PropertyToID("_MirrorBlend");
        private static readonly int MIRROR_EYE = Shader.PropertyToID("_MirrorEye");
        private static readonly int MIRROR_FLIP_Y = Shader.PropertyToID("_MirrorFlipY");
        private static readonly int FALLBACK_CUBEMAP = Shader.PropertyToID("_FallbackCubemap");
        private static readonly int FALLBACK_CUBEMAP_HDR = Shader.PropertyToID("_FallbackCubemapHDR");
        private static readonly int FALLBACK_PROBE_POS = Shader.PropertyToID("_FallbackProbePos");
        private static readonly int FALLBACK_BOX_MIN = Shader.PropertyToID("_FallbackBoxMin");
        private static readonly int FALLBACK_BOX_MAX = Shader.PropertyToID("_FallbackBoxMax");

        [SerializeField]
        [Required]
        private MeshRenderer _MeshRenderer;

        [SerializeField]
        [Tooltip("Which material slot is the mirror.")]
        [MinValue(0)]
        private int _MaterialIndex;

        [SerializeField]
        [Tooltip("Defaults to this transform. -Z faces out.")]
        private Transform _ForwardTransform;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Past this the mirror shows the fallback.")]
        [MinValue(0f)]
        private float _RenderDistance = 5f;

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
        [Tooltip("Empty uses the probe Unity picks. Set one to force it.")]
        private ReflectionProbe _FallbackProbe;

        [SerializeField]
        [Tooltip("Surfaces in the same plane that reuse this reflection.")]
        private List<MirrorSurface> _LinkedSurfaces = new();

        [PropertySpace]
        [SerializeField]
        [Tooltip("Moves the clip plane. Hides edge seams.")]
        private float _ClippingPlaneOffset;

        private const string INSTANCE_SUFFIX = " (mirror instance)";

        private static readonly Plane[] FRUSTUM_PLANES = new Plane[6];

        private static readonly Rect FULL_VIEWPORT = new(0, 0, 1, 1);

        private readonly Vector3[] _frustumCorners = new Vector3[4];

        [SerializeField]
        [HideInInspector]
        private Material _SourceMaterial;

        private Material _instance;
        private MeshFilter _meshFilter;
        private int _enabledFrame;
        private bool _valid;

        public Transform ForwardTransform => _ForwardTransform != null ? _ForwardTransform : transform;
        public float RenderDistance => _RenderDistance;
        public bool UseDepthFalloff => _UseDepthFalloff;
        public float ClippingPlaneOffset => _ClippingPlaneOffset;
        public IReadOnlyList<MirrorSurface> LinkedSurfaces => _LinkedSurfaces;
        public Material MaterialInstance => _instance;

        private void OnEnable()
        {
            _valid = Initialize();
            _enabledFrame = Time.frameCount;

            if (_valid)
                ApplyFallbackProbe();
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
                ApplyFallbackProbe();
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
            if (_MaterialIndex < 0 || _MaterialIndex >= shared.Length)
            {
                Debug.LogError($"[Mirrors] {name} material index {_MaterialIndex} is out of range (renderer has {shared.Length}).", this);
                return false;
            }

            // Skip our own instances. Stops nesting on reload.
            var slot = shared[_MaterialIndex];
            if (slot != null && slot != _instance && !slot.name.EndsWith(INSTANCE_SUFFIX))
                _SourceMaterial = slot;

            if (_SourceMaterial == null)
            {
                Debug.LogError($"[Mirrors] {name} has no material in slot {_MaterialIndex}.", this);
                return false;
            }

            // Own copy, so mirrors sharing a material get their own reflection.
            _instance = new Material(_SourceMaterial) { name = _SourceMaterial.name + INSTANCE_SUFFIX };
            SetMaterialAt(_MaterialIndex, _instance);
            return true;
        }

        private void RestoreSourceMaterial()
        {
            if (_MeshRenderer != null && _SourceMaterial != null)
                SetMaterialAt(_MaterialIndex, _SourceMaterial);

            if (_instance == null)
                return;

            if (Application.isPlaying)
                Destroy(_instance);
            else
                DestroyImmediate(_instance);

            _instance = null;
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

            // isVisible lies for the first frames after enable.
            if (Time.frameCount - _enabledFrame > 1 && !_MeshRenderer.isVisible)
                return false;

            // Behind the mirror?
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
            if (_instance == null || texture == null)
                return;

            _instance.SetTexture(eye == Camera.StereoscopicEye.Left ? MIRROR_TEX_LEFT : MIRROR_TEX_RIGHT, texture);

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetReflectionTexture(eye, texture);
            }
        }

        public void SetEye(Camera.StereoscopicEye eye)
        {
            SetForceEye(eye == Camera.StereoscopicEye.Left ? 0 : 1);
        }

        public void SetForceEye(int value)
        {
            if (_instance != null)
                _instance.SetFloat(MIRROR_EYE, value);

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
            if (_instance != null)
                _instance.SetFloat(MIRROR_FLIP_Y, flip ? 1f : 0f);

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
            if (_instance != null)
                _instance.SetFloat(MIRROR_BLEND, Mathf.Clamp01(blend));

            if (_LinkedSurfaces == null)
                return;

            foreach (var child in _LinkedSurfaces)
            {
                if (child == null || child == this)
                    continue;

                child.SetBlend(blend);
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
                return Mathf.Clamp01(_DepthFalloffCurve.Evaluate(t));
            }

            var fadeBand = Mathf.Min(_FadeLength, _RenderDistance);
            if (fadeBand <= Mathf.Epsilon)
                return distance > _RenderDistance ? 0f : _BlendStrength;

            var faded = distance - (_RenderDistance - fadeBand);
            return Mathf.Clamp01(1f - faded / fadeBand) * _BlendStrength;
        }

        // Writes the probe straight into the material.
        [Button("Reapply Probe")]
        public void ApplyFallbackProbe()
        {
            if (_instance == null)
                return;

            // No probe. Shader uses the one Unity picked.
            if (_FallbackProbe == null || _FallbackProbe.texture == null)
            {
                _instance.DisableKeyword("_PROBE_EXPLICIT");
                _instance.SetVector(FALLBACK_PROBE_POS, Vector4.zero);
                return;
            }

            _instance.EnableKeyword("_PROBE_EXPLICIT");

            var probeTransform = _FallbackProbe.transform;
            var center = (Vector4) (probeTransform.position + _FallbackProbe.center);
            var half = (Vector4) _FallbackProbe.size * 0.5f;

            var min = center - half;
            var max = center + half;
            center.w = 1f;
            min.w = 1f;
            max.w = 1f;

            _instance.SetTexture(FALLBACK_CUBEMAP, _FallbackProbe.texture);
            _instance.SetVector(FALLBACK_CUBEMAP_HDR, _FallbackProbe.textureHDRDecodeValues);
            _instance.SetVector(FALLBACK_PROBE_POS, center);
            _instance.SetVector(FALLBACK_BOX_MIN, min);
            _instance.SetVector(FALLBACK_BOX_MAX, max);
        }

        // Tight culling frustum around the mirror. False if there is nothing to cull to.
        public bool TryGetCullingMatrix(Camera reflectionCamera, MirrorProfile profile,
            Camera.MonoOrStereoscopicEye eye, out Matrix4x4 cullingMatrix)
        {
            cullingMatrix = Matrix4x4.identity;

            var mirror = ForwardTransform;
            var right = mirror.right;
            var up = mirror.up;
            var normal = -mirror.forward;
            var eyePos = reflectionCamera.transform.position;

            var planeDistance = Vector3.Dot(eyePos - mirror.position, normal);

            // Reflection cameras sit behind the mirror. Flip the axes to face it.
            // Right flips too, or the frustum turns inside out.
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
            if (TryGetViewRect(reflectionCamera, eye, right, up, normal, planeDistance, out var viewRect)
                && !Intersect(clipped, viewRect, out clipped))
                return false;

            var padX = clipped.width * profile.FrustumPadding;
            var padY = clipped.height * profile.FrustumPadding;
            clipped = Rect.MinMaxRect(clipped.xMin - padX, clipped.yMin - padY, clipped.xMax + padX, clipped.yMax + padY);

            // Near plane goes in front of the mirror. Stuff on the glass still draws.
            var near = Mathf.Max(planeDistance - profile.CullingNearOffset, planeDistance * 0.01f);
            var far = reflectionCamera.farClipPlane;
            if (near >= far)
                return false;

            // Rect was measured at the mirror. The frustum wants it at the near plane.
            var scale = near / planeDistance;

            var projection = Matrix4x4.Frustum(clipped.xMin * scale, clipped.xMax * scale,
                clipped.yMin * scale, clipped.yMax * scale, near, far);

            // View from the eye, built on the mirror axes.
            var view = Matrix4x4.identity;
            view.SetRow(0, new Vector4(right.x, right.y, right.z, -Vector3.Dot(right, eyePos)));
            view.SetRow(1, new Vector4(up.x, up.y, up.z, -Vector3.Dot(up, eyePos)));
            view.SetRow(2, new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, eyePos)));

            cullingMatrix = projection * view;
            return true;
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

            for (var i = 0; i < 4; i++)
            {
                var dir = cam.transform.TransformVector(_frustumCorners[i]);

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
        private void OnValidate()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && _instance != null)
                ApplyFallbackProbe();
        }
#endif
    }
}
