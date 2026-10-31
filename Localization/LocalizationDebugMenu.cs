// Author: František Holubec
// Created: 30.10.2026

#if DEBUG_MENU
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDIVE.Debugging;
using EDIVE.External.DomainReloadHelper;
using EDIVE.Localization.Fonts;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization
{
    // Locale switcher and the fonts it resolves to
    public static class LocalizationDebugMenu
    {
        private const string PANEL = "Localization";

        [ClearOnReload]
        private static Action _localeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (!LocalizationSettings.HasSettings)
                return;

            // Settings outlive play sessions without domain reload
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            DebugMenu.Panel(PANEL).Icon("translate");
            var section = DebugMenu.Section(PANEL, "Locale");
            section.Add(new DebugDropdown("Locale",
                    () => GetLocales().IndexOf(LocalizationSettings.SelectedLocale),
                    i => LocalizationSettings.SelectedLocale = GetLocales()[i],
                    () => GetLocales().Select(l => l.LocaleName).ToList()))
                .RefreshOn(a => _localeChanged += a, a => _localeChanged -= a);
            section.Watch("Fonts", GetFonts);

            // Characters drawn by a dynamic fallback are missing from the static atlas
            var glyphs = DebugMenu.Section(PANEL, "Dynamic glyphs");
            glyphs.Watch("Count", () => GetDynamicFonts().Sum(f => GetCharacters(f).Count()));
            glyphs.Button("Show", () => DebugMenu.ShowText("Dynamic glyphs", GetDynamicGlyphs()));
        }

        private static void OnLocaleChanged(Locale locale) => _localeChanged?.Invoke();

        private static List<Locale> GetLocales() => LocalizationSettings.AvailableLocales.Locales;

        private static string GetDynamicGlyphs()
        {
            var builder = new StringBuilder();
            foreach (var font in GetDynamicFonts().Where(f => GetCharacters(f).Any()))
            {
                builder.Append(font.name).Append(": ");
                foreach (var character in GetCharacters(font).OrderBy(c => c))
                    builder.Append(char.ConvertFromUtf32((int) character));
                builder.AppendLine();
            }
            return builder.Length > 0 ? builder.ToString() : "None";
        }

        // Icon fonts use the private use area, those are not missing text glyphs
        private static IEnumerable<uint> GetCharacters(TMP_FontAsset font) =>
            font.characterTable.Select(c => c.unicode).Where(c => c is not (>= 0xE000 and <= 0xF8FF or >= 0xF0000));

        private static List<TMP_FontAsset> GetDynamicFonts()
        {
            var fonts = new List<TMP_FontAsset>();
            foreach (var definition in Resources.FindObjectsOfTypeAll<LocalizedFontDefinition>())
                AddFallbacks(definition.CurrentFont, fonts);
            if (TMP_Settings.fallbackFontAssets != null)
                TMP_Settings.fallbackFontAssets.ForEach(f => AddFallbacks(f, fonts));
            return fonts.Where(f => f.atlasPopulationMode != AtlasPopulationMode.Static).ToList();
        }

        private static void AddFallbacks(TMP_FontAsset font, List<TMP_FontAsset> fonts)
        {
            if (font == null || fonts.Contains(font))
                return;

            fonts.Add(font);
            font.fallbackFontAssetTable?.ForEach(f => AddFallbacks(f, fonts));
        }

        private static string GetFonts()
        {
            var lines = Resources.FindObjectsOfTypeAll<LocalizedFontDefinition>()
                .Where(d => d.CurrentFont != null)
                .Select(d => $"{d.name}: {d.CurrentFont.name}, {(d.CurrentMaterial != null ? d.CurrentMaterial.name : "-")}, x{(d.CurrentOverride?.SizeScale ?? 1f):0.##}");
            return string.Join("\n", lines);
        }
    }
}
#endif
