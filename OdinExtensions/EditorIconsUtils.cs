using System;
using System.Collections.Generic;

namespace EDIVE.OdinExtensions
{
    public enum EditorIconsBundle
    {
        Odin,
        Custom,
        FontAwesome
    }

    public static class EditorIconsUtils
    {
        public const string EDITOR_ICONS_PREFIX = "oi_";
        public const string CUSTOM_ICONS_PREFIX = "cn_";
        public const string FONT_AWESOME_ICONS_PREFIX = "fa_";

        public static readonly Dictionary<string, EditorIconsBundle> ICONS_CLASS_PREFIXES = new Dictionary<string, EditorIconsBundle>()
        {
            { EDITOR_ICONS_PREFIX, EditorIconsBundle.Odin },
            { CUSTOM_ICONS_PREFIX, EditorIconsBundle.Custom },
            { FONT_AWESOME_ICONS_PREFIX, EditorIconsBundle.FontAwesome }
        };

        public static string GetPrefix(this EditorIconsBundle editorIconsBundle) => editorIconsBundle switch
        {
            EditorIconsBundle.Odin => EDITOR_ICONS_PREFIX,
            EditorIconsBundle.Custom => CUSTOM_ICONS_PREFIX,
            EditorIconsBundle.FontAwesome => FONT_AWESOME_ICONS_PREFIX,
            _ => throw new ArgumentOutOfRangeException(nameof(editorIconsBundle), editorIconsBundle, null)
        };

        public static bool TryMatchPrefix(string name, out EditorIconsBundle resultBundle, out string nameWithoutPrefix)
        {
            foreach (var prefixPair in ICONS_CLASS_PREFIXES)
            {
                if (!name.StartsWith(prefixPair.Key)) continue;
                resultBundle = prefixPair.Value;
                nameWithoutPrefix = name.Substring(prefixPair.Key.Length);
                return true;
            }
            resultBundle = default;
            nameWithoutPrefix = null;
            return false;
        }
    }
}
