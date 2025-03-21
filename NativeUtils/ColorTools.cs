using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "For convenient usage")]
    public static class ColorTools
    {
        public static readonly Color Red       = new(0.9f,   0.05f, 0.05f);
        public static readonly Color Orange    = new(0.9f,   0.45f, 0.05f);
        public static readonly Color Yellow    = new(1f,     1f,    0f);
        public static readonly Color Lime      = new(0.65f,  0.9f,  0.05f);
        public static readonly Color Green     = new(0.29f,  0.9f,  0.05f);
        public static readonly Color Cyan      = new(0f,     1f,    0.75f);
        public static readonly Color Aqua      = new(0.05f,  0.75f, 0.9f);
        public static readonly Color Blue      = new(0.05f,  0.26f, 0.9f);
        public static readonly Color Purple    = new(0.65f,  0.15f, 1f);
        public static readonly Color Magenta   = new(0.95f,  0.05f, 0.85f);
        public static readonly Color Pink      = new(1f,     0f,    0.5f);

        public static readonly Color White     = new(1,      1,     1);
        public static readonly Color LightGrey = new(0.75f,  0.75f, 0.75f);
        public static readonly Color Grey      = new(0.5f,   0.5f,  0.5f);
        public static readonly Color DarkGrey  = new(0.25f,  0.25f, 0.25f);
        public static readonly Color Black     = new(0,      0,     0);

        public static readonly Color Error     = new(1f,   0.32f, 0.29f);
        public static readonly Color Warning   = new(1f,   0.75f, 0.02f);

        public static Color GetRainbowColor(int index, int count)
        {
            if (count <= 0 || index < 0 || index >= count)
                return Red;

            var hue = (float) index / count;
            return Color.HSVToRGB(hue, 0.95f, 1);
        }
    }
}
