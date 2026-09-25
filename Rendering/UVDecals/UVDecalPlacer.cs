// Author: František Holubec
// Created: 22.09.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

namespace EDIVE.Rendering.UVDecals
{
    [ExecuteAlways]
    public class UVDecalPlacer : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private UVDecalPainter _Painter;

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, ListElementLabelName = nameof(UVDecalArea.EditorLabel))]
        private List<UVDecalArea> _Areas = new();

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, ListElementLabelName = nameof(UVDecalPreset.EditorLabel))]
        private List<UVDecalPreset> _Decals = new();

        private readonly List<UVDecal> _resolved = new();

        public IReadOnlyList<UVDecalArea> Areas => _Areas;
        public IReadOnlyList<UVDecalPreset> Decals => _Decals;

        public void SetDecals(IEnumerable<UVDecalPreset> decals)
        {
            _Decals.Clear();
            if (decals != null)
                _Decals.AddRange(decals);
            Rebuild();
        }

        public void SetDecal(UVDecalPreset decal)
        {
            _Decals.Clear();
            if (decal != null)
                _Decals.Add(decal);
            Rebuild();
        }

        public void AddDecal(UVDecalPreset decal)
        {
            _Decals.Add(decal);
            Rebuild();
        }

        public void Clear()
        {
            _Decals.Clear();
            Rebuild();
        }

        public bool TryGetArea(UVDecalPlacement placement, out UVDecalArea area)
        {
            if (placement != null)
            {
                foreach (var candidate in _Areas)
                {
                    if (candidate._Placement != placement) continue;
                    area = candidate;
                    return true;
                }
            }
            area = default;
            return false;
        }

        [Button("Rebuild")]
        public void Rebuild()
        {
            if (_Painter == null || !isActiveAndEnabled) return;

            _resolved.Clear();

#if UNITY_EDITOR
            if (!Application.isPlaying && ShowAreaPreviews)
            {
                AppendAreaPreviews();
                _Painter.SetExtraDecals(_resolved, exclusive: true);
                return;
            }
#endif

            foreach (var preset in _Decals)
            {
                if (preset == null || preset._Texture == null) continue;

                if (!TryGetArea(preset._Placement, out var area))
                {
                    var placementName = preset._Placement != null ? preset._Placement.name : "none";
                    Debug.LogWarning($"{name}: no UV decal area for placement '{placementName}', decal skipped.", this);
                    continue;
                }

                _resolved.Add(Resolve(area, preset));
            }

            _Painter.SetExtraDecals(_resolved);
        }
        
        private static UVDecal Resolve(in UVDecalArea area, in UVDecalPreset preset)
        {
            var totalRotation = area._Rotation + preset._Rotation;
            var fitSize = FitSize(area._Size, preset._Texture);
            var size = new Vector2(fitSize.x * preset._Scale.x, fitSize.y * preset._Scale.y);

            var half = new Vector2(0.5f, 0.5f);
            var anchorLocal = (preset._Anchor - half) * area._Size;
            var anchorUV = area._Center + Rotate(anchorLocal, area._Rotation);

            var pivotLocal = (preset._Pivot - half) * size;
            var pivotOffset = Rotate(pivotLocal, totalRotation);

            return new UVDecal
            {
                _Texture = preset._Texture,
                _Channel = area._Channel,
                _Center = anchorUV - pivotOffset,
                _Size = size,
                _Rotation = totalRotation,
                _Tint = preset._Tint,
                _OverrideSmoothness = preset._OverrideSmoothness,
                _Smoothness = preset._Smoothness
            };
        }

        // Max size that fits the texture's own aspect ratio inside the area, preserving it. _Scale then scales this.
        private static Vector2 FitSize(Vector2 areaSize, Texture texture)
        {
            var extents = new Vector2(Mathf.Abs(areaSize.x), Mathf.Abs(areaSize.y));
            if (texture == null || texture.height <= 0 || extents.y <= 0f)
                return areaSize;

            var textureAspect = (float) texture.width / texture.height;
            var areaAspect = extents.x / extents.y;
            var fitted = textureAspect >= areaAspect
                ? new Vector2(extents.x, extents.x / textureAspect)
                : new Vector2(extents.y * textureAspect, extents.y);

            return new Vector2(fitted.x * Mathf.Sign(areaSize.x), fitted.y * Mathf.Sign(areaSize.y));
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
            if (_Painter != null)
                _Painter.SetExtraDecals(null);
        }

        private void Reset()
        {
            _Painter = GetComponent<UVDecalPainter>();
        }

#if UNITY_EDITOR
        private static GlobalPersistentContext<bool> ShowAreaPreviewsContext => PersistentContext.Get("UVDecalPlacer.ShowAreaPreviews", false);

        [PropertyOrder(-1)]
        [ShowInInspector]
        private bool ShowAreaPreviews
        {
            get => ShowAreaPreviewsContext.Value;
            set
            {
                ShowAreaPreviewsContext.Value = value;
                Rebuild();
            }
        }

        private void AppendAreaPreviews()
        {
            foreach (var area in _Areas)
            {
                if (area._Placement == null) continue;
                if (_resolved.Count >= UVDecalPainter.MAX_DECALS) break;

                _resolved.Add(new UVDecal
                {
                    _Texture = GetCheckerTexture(),
                    _Channel = area._Channel,
                    _Center = area._Center,
                    _Size = area._Size,
                    _Rotation = area._Rotation,
                    _Tint = new Color(1f, 1f, 1f, 0.5f),
                    _OverrideSmoothness = false
                });
            }
        }

        private const int CHECKER_TEX_SIZE = 64;
        private const int CHECKER_TEX_CELL = 8;
        private static Texture2D _checkerTexture;

        private static Texture2D GetCheckerTexture()
        {
            if (_checkerTexture != null) return _checkerTexture;

            var pixels = new Color32[CHECKER_TEX_SIZE * CHECKER_TEX_SIZE];
            for (var y = 0; y < CHECKER_TEX_SIZE; y++)
            {
                for (var x = 0; x < CHECKER_TEX_SIZE; x++)
                {
                    var checker = (x / CHECKER_TEX_CELL + y / CHECKER_TEX_CELL) % 2 == 0;
                    pixels[y * CHECKER_TEX_SIZE + x] = checker ? Color.black : Color.white;
                }
            }

            _checkerTexture = new Texture2D(CHECKER_TEX_SIZE, CHECKER_TEX_SIZE, TextureFormat.RGBA32, false)
            {
                name = "UVDecalPlacer Area Checker Preview",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            _checkerTexture.SetPixels32(pixels);
            _checkerTexture.Apply(false, false);
            return _checkerTexture;
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Rebuild();
            };
        }
#endif
    }
}
