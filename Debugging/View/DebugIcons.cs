// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using EDIVE.External.DomainReloadHelper;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Icon labels by symbol name from the settings icon font: "terminal", "lan", "group".
    public static class DebugIcons
    {
        public const string ICON_CLASS = "dm-icon";

        [ClearOnReload]
        private static Dictionary<string, string> _glyphs;

        public static Label Create(string name, string className = null)
        {
            var label = new Label(GetGlyph(name)) {pickingMode = PickingMode.Ignore};
            label.AddToClassList(ICON_CLASS);
            if (className != null)
                label.AddToClassList(className);

            var font = DebugMenuSettings.Default?.Icons?.Font;
            if (font != null)
                label.style.unityFontDefinition = FontDefinition.FromFont(font);
            return label;
        }

        public static void Set(Label label, string name) => label.text = GetGlyph(name);

        public static string GetGlyph(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            if (_glyphs == null)
            {
                _glyphs = new Dictionary<string, string>();
                var codepoints = DebugMenuSettings.Default?.Icons?.Codepoints;
                if (codepoints != null)
                {
                    foreach (var entry in codepoints.Entries)
                    {
                        if (entry.Unicode != 0 && !string.IsNullOrEmpty(entry.Name))
                            _glyphs.TryAdd(entry.Name, char.ConvertFromUtf32((int) entry.Unicode));
                    }
                }
            }

            return _glyphs.TryGetValue(name, out var glyph) ? glyph : string.Empty;
        }
    }
}
