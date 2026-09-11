// Author: František Holubec
// Created: 02.09.2026

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Rendering.UVDecals
{
    [ExecuteAlways]
    public class UVDecalPainter : MonoBehaviour
    {
        // Must match MAX_UV_DECALS in LitUVDecalsInput.hlsl.
        private const int MAX_DECALS = 2;

        [SerializeField]
        [Required]
        [Tooltip("Renderer whose material uses the 'BioPharmaHub/Lit UV Decals' shader.")]
        private Renderer _Renderer;

        [SerializeField]
        [Min(0)]
        [Tooltip("Material / submesh index on that renderer.")]
        private int _MaterialIndex;

        [SerializeField]
        [EnhancedInlineEditor]
        [Tooltip("Optional. When set, its decals are used and the list below is ignored.")]
        private UVDecalPreset _Preset;

        [SerializeField]
        [HideIf(nameof(_Preset))]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<UVDecal> _Decals = new();

        [SerializeField]
        [Tooltip("Resolution each decal texture is resampled to for the texture array.")]
        private int _Resolution = 512;

        private static readonly int DECALS_ID = Shader.PropertyToID("_UVDecals");
        private static readonly int RECT_ID = Shader.PropertyToID("_UVDecalRect");
        private static readonly int ROT_ID = Shader.PropertyToID("_UVDecalRot");
        private static readonly int TINT_ID = Shader.PropertyToID("_UVDecalTint");
        private static readonly int COUNT_ID = Shader.PropertyToID("_UVDecalCount");

        private MaterialPropertyBlock _block;
        private Texture2DArray _array;
        private int[] _textureKeys;

        private readonly Vector4[] _rect = new Vector4[MAX_DECALS];
        private readonly Vector4[] _rot = new Vector4[MAX_DECALS];
        private readonly Vector4[] _tint = new Vector4[MAX_DECALS];
        
        public IReadOnlyList<UVDecal> ActiveDecals => _Preset != null ? _Preset.Decals : _Decals;
        
        public void ApplyPreset(UVDecalPreset preset)
        {
            _Preset = preset;
            Rebuild();
        }
        
        public void SetDecals(IEnumerable<UVDecal> decals)
        {
            GoLocal();
            _Decals.Clear();
            _Decals.AddRange(decals);
            Rebuild();
        }

        public void AddDecal(UVDecal decal)
        {
            GoLocal();
            _Decals.Add(decal);
            Rebuild();
        }

        public void RemoveDecalAt(int index)
        {
            GoLocal();
            if (index < 0 || index >= _Decals.Count) return;
            _Decals.RemoveAt(index);
            Rebuild();
        }
        
        public void Clear()
        {
            _Preset = null;
            _Decals.Clear();
            Rebuild();
        }

        private void GoLocal()
        {
            if (_Preset == null) return;
            _Decals.Clear();
            _Decals.AddRange(_Preset.Decals);
            _Preset = null;
        }

        private void OnEnable()
        {
            Rebuild();
#if UNITY_EDITOR
            UVDecalPreset.Changed += OnPresetChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UVDecalPreset.Changed -= OnPresetChanged;
#endif
            if (_Renderer != null)
                _Renderer.SetPropertyBlock(null, _MaterialIndex);
        }

        private void OnDestroy() => DestroyArray();

#if UNITY_EDITOR
        private void OnPresetChanged(UVDecalPreset preset)
        {
            if (preset == _Preset) Rebuild();
        }
#endif

        [Button("Rebuild")]
        public void Rebuild()
        {
            if (_Renderer == null) return;
            if (_MaterialIndex >= Mathf.Max(1, _Renderer.sharedMaterials.Length)) return;

            _block ??= new MaterialPropertyBlock();

            var decals = ActiveDecals;
            var count = Mathf.Min(decals.Count, MAX_DECALS);
            if (decals.Count > MAX_DECALS)
                Debug.LogWarning($"{name}: {decals.Count} UV decals set, only the first {MAX_DECALS} are used.", this);

            if (count == 0)
                DestroyArray();
            else
                EnsureTextureArray(decals, count);

            for (var i = 0; i < MAX_DECALS; i++)
            {
                if (i < count)
                {
                    var d = decals[i];
                    var rad = -d._Rotation * Mathf.Deg2Rad;
                    _rect[i] = new Vector4(d._Center.x, d._Center.y, Mathf.Max(0f, d._Size.x), Mathf.Max(0f, d._Size.y));
                    _rot[i] = new Vector4(Mathf.Cos(rad), Mathf.Sin(rad), i, (float) d._Channel);
                    _tint[i] = d._Tint;
                }
                else
                {
                    _rect[i] = Vector4.zero;
                    _rot[i] = new Vector4(1f, 0f, 0f, 0f);
                    _tint[i] = Vector4.zero;
                }
            }

            _Renderer.GetPropertyBlock(_block, _MaterialIndex);
            if (_array != null)
                _block.SetTexture(DECALS_ID, _array);
            _block.SetVectorArray(RECT_ID, _rect);
            _block.SetVectorArray(ROT_ID, _rot);
            _block.SetVectorArray(TINT_ID, _tint);
            _block.SetFloat(COUNT_ID, count);
            _Renderer.SetPropertyBlock(_block, _MaterialIndex);
        }

        private void EnsureTextureArray(IReadOnlyList<UVDecal> decals, int count)
        {
            var keys = new int[count];
            for (var i = 0; i < count; i++)
            {
                var tex = decals[i]._Texture;
                keys[i] = tex != null ? tex.GetInstanceID() : 0;
            }

            if (_array != null && _array.depth == count && _array.width == _Resolution &&
                _textureKeys != null && ArraysEqual(_textureKeys, keys))
                return;

            DestroyArray();

            _array = new Texture2DArray(_Resolution, _Resolution, count, TextureFormat.RGBA32, true, false)
            {
                name = $"{name}_UVDecals",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var rt = RenderTexture.GetTemporary(_Resolution, _Resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var readback = new Texture2D(_Resolution, _Resolution, TextureFormat.RGBA32, true, false);
            var prev = RenderTexture.active;

            for (var i = 0; i < count; i++)
            {
                var src = decals[i]._Texture != null ? (Texture) decals[i]._Texture : Texture2D.blackTexture;
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                readback.ReadPixels(new Rect(0, 0, _Resolution, _Resolution), 0, 0, false);
                readback.Apply(true, false);
                Graphics.CopyTexture(readback, 0, _array, i);
            }

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            SafeDestroy(readback);

            _array.Apply(false, true);
            _textureKeys = keys;
        }

        private void DestroyArray()
        {
            if (_array == null) return;
            SafeDestroy(_array);
            _array = null;
            _textureKeys = null;
        }

        private static bool ArraysEqual(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void SafeDestroy(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            _Resolution = Mathf.Clamp(Mathf.ClosestPowerOfTwo(_Resolution), 16, 2048);
            _MaterialIndex = Mathf.Max(0, _MaterialIndex);
            if (!isActiveAndEnabled) return;
            EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Rebuild();
            };
        }
#endif
    }
}
