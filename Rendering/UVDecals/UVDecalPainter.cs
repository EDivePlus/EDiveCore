// Author: František Holubec
// Created: 02.09.2026

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Rendering.UVDecals
{
    [ExecuteAlways]
    public class UVDecalPainter : MonoBehaviour
    {
        // Same as slots in UVDecals.hlsl.
        public const int MAX_DECALS = 4;

        [SerializeField]
        [Required]
        [Tooltip("Uses EDIVE/Lit UV Decals.")]
        private Renderer _Renderer;

        [SerializeField]
        [Min(0)]
        [Tooltip("Submesh.")]
        private int _MaterialIndex;
        
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<UVDecal> _Decals = new();

        private static readonly int[] TEX_IDS = CreateIds("Tex");
        private static readonly int[] RECT_IDS = CreateIds("Rect");
        private static readonly int[] PARAMS_IDS = CreateIds("Params");
        private static readonly int[] TINT_IDS = CreateIds("Tint");

        private Material _sourceMaterial;
        private Material _instance;

        public IReadOnlyList<UVDecal> ActiveDecals => _Decals;
        
        public void SetDecals(IEnumerable<UVDecal> decals)
        {
            _Decals.Clear();
            _Decals.AddRange(decals);
            Rebuild();
        }

        public void AddDecal(UVDecal decal)
        {
            _Decals.Add(decal);
            Rebuild();
        }

        public void RemoveDecalAt(int index)
        {
            if (index < 0 || index >= _Decals.Count) return;
            _Decals.RemoveAt(index);
            Rebuild();
        }

        public void Clear()
        {
            _Decals.Clear();
            Rebuild();
        }
        
        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            ClearPreview();
#endif
            RestoreSourceMaterial();
        }

        private void OnDestroy()
        {
            SafeDestroy(_instance);
            _instance = null;
        }

        [Button("Rebuild")]
        public void Rebuild()
        {
            if (_Renderer == null || !isActiveAndEnabled) return;

            var decals = ActiveDecals;
            if (decals.Count > MAX_DECALS)
                Debug.LogWarning($"{name}: {decals.Count} UV decals set, only the first {MAX_DECALS} are used.", this);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                WritePreview(decals);
                return;
            }
            ClearPreview();
#endif

            var material = GetInstance();
            if (material == null) return;

            for (var i = 0; i < MAX_DECALS; i++)
            {
                GetSlot(decals, i, out var texture, out var rect, out var parameters, out var tint);
                material.SetTexture(TEX_IDS[i], texture);
                material.SetVector(RECT_IDS[i], rect);
                material.SetVector(PARAMS_IDS[i], parameters);
                material.SetColor(TINT_IDS[i], tint);
            }
        }

        private Material GetInstance()
        {
            var materials = _Renderer.sharedMaterials;
            if (_MaterialIndex >= materials.Length) return null;

            var current = materials[_MaterialIndex];
            if (current == null) return null;

            if (_instance == null || (current != _instance && current != _sourceMaterial))
            {
                SafeDestroy(_instance);
                _sourceMaterial = current;
                _instance = new Material(current) { name = $"{current.name} (UV Decals)" };
            }

            if (current != _instance)
            {
                materials[_MaterialIndex] = _instance;
                _Renderer.sharedMaterials = materials;
            }
            return _instance;
        }

        private void RestoreSourceMaterial()
        {
            if (_Renderer == null || _instance == null || _sourceMaterial == null) return;

            var materials = _Renderer.sharedMaterials;
            if (_MaterialIndex >= materials.Length || materials[_MaterialIndex] != _instance) return;

            materials[_MaterialIndex] = _sourceMaterial;
            _Renderer.sharedMaterials = materials;
        }

        private static void GetSlot(IReadOnlyList<UVDecal> decals, int index, out Texture texture, out Vector4 rect, out Vector4 parameters, out Color tint)
        {
            if (index >= decals.Count || decals[index]._Texture == null)
            {
                texture = Texture2D.blackTexture;
                rect = Vector4.zero;
                parameters = new Vector4(1f, 0f, 0f, -1f);
                tint = Color.clear;
                return;
            }

            var decal = decals[index];
            var rad = -decal._Rotation * Mathf.Deg2Rad;
            texture = decal._Texture;
            rect = new Vector4(decal._Center.x, decal._Center.y, Mathf.Max(0f, decal._Size.x), Mathf.Max(0f, decal._Size.y));
            parameters = new Vector4(Mathf.Cos(rad), Mathf.Sin(rad), (float) decal._Channel, decal._OverrideSmoothness ? decal._Smoothness : -1f);
            tint = decal._Tint;
        }

        private static int[] CreateIds(string suffix)
        {
            var ids = new int[MAX_DECALS];
            for (var i = 0; i < MAX_DECALS; i++)
                ids[i] = Shader.PropertyToID($"_UVDecal{i}{suffix}");
            return ids;
        }

        private static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

#if UNITY_EDITOR
        private MaterialPropertyBlock _previewBlock;

        // Preview without dirtying the scene
        private void WritePreview(IReadOnlyList<UVDecal> decals)
        {
            if (_MaterialIndex >= _Renderer.sharedMaterials.Length) return;

            _previewBlock ??= new MaterialPropertyBlock();
            _Renderer.GetPropertyBlock(_previewBlock, _MaterialIndex);
            for (var i = 0; i < MAX_DECALS; i++)
            {
                GetSlot(decals, i, out var texture, out var rect, out var parameters, out var tint);
                _previewBlock.SetTexture(TEX_IDS[i], texture);
                _previewBlock.SetVector(RECT_IDS[i], rect);
                _previewBlock.SetVector(PARAMS_IDS[i], parameters);
                _previewBlock.SetColor(TINT_IDS[i], tint);
            }
            _Renderer.SetPropertyBlock(_previewBlock, _MaterialIndex);
        }

        private void ClearPreview()
        {
            if (_Renderer != null && _MaterialIndex < _Renderer.sharedMaterials.Length)
                _Renderer.SetPropertyBlock(null, _MaterialIndex);
        }

        private void OnValidate()
        {
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
