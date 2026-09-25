// Author: František Holubec
// Created: 02.09.2026

using System.Collections.Generic;
using EDIVE.NativeUtils;
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

#if UNITY_EDITOR
        [ShowInInspector]
        [ReadOnly]
        [ListDrawerSettings(ShowFoldout = false)]
        [Tooltip("Decals fed in externally, e.g. by a UVDecalPlacer. Not serialized.")]
        private List<UVDecal> DynamicDecals => _extraDecals;
#endif

        private static readonly int[] TEX_IDS = CreateIds("Tex");
        private static readonly int[] RECT_IDS = CreateIds("Rect");
        private static readonly int[] PARAMS_IDS = CreateIds("Params");
        private static readonly int[] TINT_IDS = CreateIds("Tint");

        private MaterialSlotOverride _override;

        private readonly List<UVDecal> _extraDecals = new();
        private readonly List<UVDecal> _activeDecals = new();
        private bool _extraExclusive;

        public IReadOnlyList<UVDecal> ActiveDecals => _activeDecals;

        // exclusive: extras replace _Decals instead of adding to them.
        public void SetExtraDecals(IEnumerable<UVDecal> decals, bool exclusive = false)
        {
            _extraDecals.Clear();
            if (decals != null)
                _extraDecals.AddRange(decals);
            _extraExclusive = exclusive;
            Rebuild();
        }

        public void SetDecals(IEnumerable<UVDecal> decals)
        {
            _Decals.Clear();
            if (decals != null)
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
            MaterialSlotOverride.Release(ref _override);
        }

        [Button("Rebuild")]
        public void Rebuild()
        {
            if (_Renderer == null || !isActiveAndEnabled) return;

            _activeDecals.Clear();
            if (!_extraExclusive)
                _activeDecals.AddRange(_Decals);
            _activeDecals.AddRange(_extraDecals);

            var decals = ActiveDecals;
            if (decals.Count > MAX_DECALS)
                Debug.LogWarning($"{name}: {decals.Count} UV decals set, only the first {MAX_DECALS} are used.", this);

            // Edit mode writes a property block, so the scene stays clean.
            if (!MaterialSlotOverride.Ensure(ref _override, _Renderer, _MaterialIndex, " (UV Decals)")) return;

            for (var i = 0; i < MAX_DECALS; i++)
            {
                GetSlot(decals, i, out var texture, out var rect, out var parameters, out var tint);
                _override.SetTexture(TEX_IDS[i], texture);
                _override.SetVector(RECT_IDS[i], rect);
                _override.SetVector(PARAMS_IDS[i], parameters);
                _override.SetColor(TINT_IDS[i], tint);
            }
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
            rect = new Vector4(decal._Center.x, decal._Center.y, decal._Size.x, decal._Size.y);
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

#if UNITY_EDITOR
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
