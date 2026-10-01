// Author: František Holubec
// Created: 01.10.2026

using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TextCore;

namespace EDIVE.Rendering.UVDecals
{
    // Bakes text from a TMP SDF font into a white texture with coverage in alpha, usable as a UV decal.
    public static class UVDecalTextBaker
    {
        private static readonly Dictionary<TMP_FontAsset, TMP_FontAsset> WorkingFonts = new();

        private readonly struct PlacedGlyph
        {
            public readonly GlyphRect Rect;
            public readonly float Left;
            public readonly float Bottom;
            public readonly Vector2 AtlasPerUnit;

            public PlacedGlyph(Glyph glyph, float cursor)
            {
                var metrics = glyph.metrics;
                Rect = glyph.glyphRect;
                Left = cursor + metrics.horizontalBearingX;
                Bottom = metrics.horizontalBearingY - metrics.height;
                AtlasPerUnit = new Vector2(
                    metrics.width > 0f ? Rect.width / metrics.width : 1f,
                    metrics.height > 0f ? Rect.height / metrics.height : 1f);
            }
        }

        // Returns null for empty text. Reuses target when given.
        public static Texture2D Bake(TMP_FontAsset font, string text, int height, Texture2D target = null)
        {
            if (font == null || string.IsNullOrEmpty(text) || height <= 0)
                return null;

            font = GetWorkingFont(font);
            if (font.atlasPopulationMode != AtlasPopulationMode.Static)
                font.TryAddCharacters(text);

            var atlas = font.atlasTexture;
            if (atlas == null || !atlas.isReadable || atlas.format != TextureFormat.Alpha8)
            {
                Debug.LogWarning($"UV decal text: atlas of '{font.name}' must be a readable Alpha8 texture.", font);
                return null;
            }

            var glyphs = new List<PlacedGlyph>();
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var cursor = 0f;
            foreach (var c in text)
            {
                if (!font.characterLookupTable.TryGetValue(c, out var character))
                    continue;

                var placed = new PlacedGlyph(character.glyph, cursor);
                var metrics = character.glyph.metrics;
                min = Vector2.Min(min, new Vector2(placed.Left, placed.Bottom));
                max = Vector2.Max(max, new Vector2(placed.Left + metrics.width, placed.Bottom + metrics.height));
                glyphs.Add(placed);
                cursor += metrics.horizontalAdvance;
            }

            if (glyphs.Count == 0)
                return null;

            var margin = (max.y - min.y) * 0.04f;
            min -= Vector2.one * margin;
            max += Vector2.one * margin;
            var unitsPerPixel = (max.y - min.y) / height;
            var width = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / unitsPerPixel));

            var sdf = atlas.GetPixelData<byte>(0);
            var padding = font.atlasPadding;
            var gradientScale = padding + 1f;
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = min + new Vector2(x + 0.5f, y + 0.5f) * unitsPerPixel;
                    var distance = float.MinValue;
                    var atlasPixel = unitsPerPixel;

                    foreach (var glyph in glyphs)
                    {
                        var ax = glyph.Rect.x + (point.x - glyph.Left) * glyph.AtlasPerUnit.x;
                        var ay = glyph.Rect.y + (point.y - glyph.Bottom) * glyph.AtlasPerUnit.y;
                        if (ax < glyph.Rect.x - padding || ax > glyph.Rect.x + glyph.Rect.width + padding ||
                            ay < glyph.Rect.y - padding || ay > glyph.Rect.y + glyph.Rect.height + padding)
                            continue;

                        var d = (SampleBilinear(sdf, atlas.width, atlas.height, ax, ay) - 0.5f) * gradientScale;
                        if (d <= distance) continue;
                        distance = d;
                        atlasPixel = unitsPerPixel * glyph.AtlasPerUnit.y;
                    }

                    var coverage = Mathf.Clamp01(distance / atlasPixel + 0.5f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte) Mathf.RoundToInt(coverage * 255f));
                }
            }

            if (target == null)
            {
                target = new Texture2D(width, height, TextureFormat.RGBA32, true)
                {
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
            else if (target.width != width || target.height != height)
            {
                target.Reinitialize(width, height, TextureFormat.RGBA32, true);
            }

            target.name = $"UV Decal Text '{text}'";
            target.SetPixels32(pixels);
            target.Apply(true);
            return target;
        }

        // Dynamic fonts get an in-memory copy, so baking never writes glyphs into the font asset.
        private static TMP_FontAsset GetWorkingFont(TMP_FontAsset font)
        {
            if (font.atlasPopulationMode == AtlasPopulationMode.Static || font.sourceFontFile == null)
                return font;

            if (WorkingFonts.TryGetValue(font, out var copy) && copy != null)
                return copy;

            copy = TMP_FontAsset.CreateFontAsset(font.sourceFontFile, Mathf.RoundToInt(font.faceInfo.pointSize), font.atlasPadding,
                font.atlasRenderMode, font.atlasWidth, font.atlasHeight, AtlasPopulationMode.Dynamic, false);
            if (copy == null)
                return font;

            copy.name = $"{font.name} (UV Decal Text)";
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.atlasTexture.hideFlags = HideFlags.HideAndDontSave;
            copy.material.hideFlags = HideFlags.HideAndDontSave;
            WorkingFonts[font] = copy;
            return copy;
        }

        private static float SampleBilinear(NativeArray<byte> data, int width, int height, float x, float y)
        {
            x -= 0.5f;
            y -= 0.5f;
            var x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, width - 1);
            var y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, height - 1);
            var x1 = Mathf.Min(x0 + 1, width - 1);
            var y1 = Mathf.Min(y0 + 1, height - 1);
            var tx = Mathf.Clamp01(x - x0);
            var ty = Mathf.Clamp01(y - y0);
            var bottom = Mathf.Lerp(data[y0 * width + x0], data[y0 * width + x1], tx);
            var top = Mathf.Lerp(data[y1 * width + x0], data[y1 * width + x1], tx);
            return Mathf.Lerp(bottom, top, ty) / 255f;
        }
    }
}
