// Author: František Holubec
// Created: 30.10.2026

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace EDIVE.Localization.Fonts
{
    // Checks which characters of the string tables a font and its fallbacks can draw.
    public static class FontGlyphCoverage
    {
        private const int MAX_LISTED = 40;

        public static HashSet<uint> GetLocaleCharacters(LocaleIdentifier locale)
        {
            var characters = new HashSet<uint>();
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                if (collection.GetTable(locale) is not StringTable table)
                    continue;

                foreach (var entry in table.Values)
                    AddCharacters(entry?.Value, characters);
            }
            return characters;
        }

        // Missing can't be drawn at all, dynamic is drawn only by a dynamic font at runtime.
        public static void Check(TMP_FontAsset font, IEnumerable<uint> characters, List<uint> missing, List<uint> dynamic)
        {
            var fonts = GetFontChain(font);
            var remaining = characters.Where(c => !fonts.Any(f => !IsDynamic(f) && f.characterLookupTable.ContainsKey(c))).ToList();

            foreach (var dynamicFont in fonts.Where(f => IsDynamic(f) && f.sourceFontFile != null))
            {
                if (FontEngine.LoadFontFace(dynamicFont.sourceFontFile) != FontEngineError.Success)
                    continue;

                foreach (var character in remaining.Where(c => FontEngine.TryGetGlyphIndex(c, out var index) && index != 0).ToList())
                {
                    remaining.Remove(character);
                    dynamic.Add(character);
                }
            }

            missing.AddRange(remaining);
        }

        public static string Format(List<uint> characters)
        {
            var builder = new StringBuilder();
            foreach (var character in characters.OrderBy(c => c).Take(MAX_LISTED))
            {
                if (character < 0x21 || character is >= 0x7F and <= 0xA0 || character is >= 0x2000 and <= 0x200F)
                    builder.Append($"\\u{character:X4}");
                else
                    builder.Append(char.ConvertFromUtf32((int) character));
            }

            if (characters.Count > MAX_LISTED)
                builder.Append($" (+{characters.Count - MAX_LISTED})");
            return builder.ToString();
        }

        public static string ToText(IEnumerable<uint> characters)
        {
            var builder = new StringBuilder();
            foreach (var character in characters.OrderBy(c => c))
                builder.Append(char.ConvertFromUtf32((int) character));
            return builder.ToString();
        }

        public static void AddCharacters(string text, HashSet<uint> characters)
        {
            if (string.IsNullOrEmpty(text))
                return;

            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsControl(text[i]))
                    continue;

                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    characters.Add((uint) char.ConvertToUtf32(text[i], text[i + 1]));
                    i++;
                }
                else
                {
                    characters.Add(text[i]);
                }
            }
        }

        private static List<TMP_FontAsset> GetFontChain(TMP_FontAsset font)
        {
            var fonts = new List<TMP_FontAsset>();
            AddFont(font, fonts);
            if (TMP_Settings.fallbackFontAssets != null)
            {
                foreach (var fallback in TMP_Settings.fallbackFontAssets)
                    AddFont(fallback, fonts);
            }
            return fonts;
        }

        private static void AddFont(TMP_FontAsset font, List<TMP_FontAsset> fonts)
        {
            if (font == null || fonts.Contains(font))
                return;

            fonts.Add(font);
            if (font.fallbackFontAssetTable == null)
                return;

            foreach (var fallback in font.fallbackFontAssetTable)
                AddFont(fallback, fonts);
        }

        private static bool IsDynamic(TMP_FontAsset font) => font.atlasPopulationMode != AtlasPopulationMode.Static;
    }
}
#endif
