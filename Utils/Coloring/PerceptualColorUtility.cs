// Author: František Holubec
// Created: 14.10.2026

using UnityEngine;

namespace EDIVE.Utils.Coloring
{
    // Colors in OKLCH: lightness (0-1), chroma (colorfulness, 0-~0.37) and hue (degrees).
    // Equal steps look equally different, unlike RGB or HSV
    public static class PerceptualColorUtility
    {
        private const int GAMUT_STEPS = 16;
        private const float GRAY_CHROMA = 0.03f;

        private static readonly (string Name, float Hue)[] HUE_NAMES =
        {
            ("Rose", 12), ("Red", 25), ("Orange", 57), ("Amber", 80), ("Yellow", 97), ("Lime", 128),
            ("Green", 148), ("Emerald", 165), ("Teal", 182), ("Cyan", 210), ("Sky", 233), ("Blue", 258),
            ("Indigo", 277), ("Violet", 292), ("Purple", 304), ("Fuchsia", 322), ("Pink", 352)
        };

        // Out of gamut colors keep lightness and hue, chroma is lowered
        public static Color ToColor(float lightness, float chroma, float hue, float alpha = 1f)
        {
            if (TryToColor(lightness, chroma, hue, alpha, out var color))
                return color;

            var low = 0f;
            var high = chroma;
            for (var i = 0; i < GAMUT_STEPS; i++)
            {
                var mid = (low + high) * 0.5f;
                if (TryToColor(lightness, mid, hue, alpha, out _))
                    low = mid;
                else
                    high = mid;
            }
            TryToColor(lightness, low, hue, alpha, out color);
            return color;
        }

        public static (float Lightness, float Chroma, float Hue) FromColor(Color color)
        {
            var linear = color.linear;
            var l = Cbrt(0.4122214708f * linear.r + 0.5363325363f * linear.g + 0.0514459929f * linear.b);
            var m = Cbrt(0.2119034982f * linear.r + 0.6806995451f * linear.g + 0.1073969566f * linear.b);
            var s = Cbrt(0.0883024619f * linear.r + 0.2817188376f * linear.g + 0.6299787005f * linear.b);

            var lightness = 0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s;
            var a = 1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s;
            var b = 0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s;

            var hue = Mathf.Atan2(b, a) * Mathf.Rad2Deg;
            return (lightness, Mathf.Sqrt(a * a + b * b), hue < 0 ? hue + 360f : hue);
        }

        // Nearest Tailwind hue family, e.g. "Orange" or "Light Sky"
        public static string GetName(Color color)
        {
            var (lightness, chroma, hue) = FromColor(color);
            var name = chroma < GRAY_CHROMA ? "Gray" : NearestHueName(hue);
            return lightness switch
            {
                > 0.9f => "Pale " + name,
                < 0.45f => "Dark " + name,
                _ => name
            };
        }

        private static string NearestHueName(float hue)
        {
            var best = HUE_NAMES[0].Name;
            var bestDistance = float.MaxValue;
            foreach (var (name, center) in HUE_NAMES)
            {
                var distance = Mathf.Abs(Mathf.DeltaAngle(hue, center));
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = name;
            }
            return best;
        }

        private static bool TryToColor(float lightness, float chroma, float hue, float alpha, out Color color)
        {
            var radians = hue * Mathf.Deg2Rad;
            var a = chroma * Mathf.Cos(radians);
            var b = chroma * Mathf.Sin(radians);

            var l = Cube(lightness + 0.3963377774f * a + 0.2158037573f * b);
            var m = Cube(lightness - 0.1055613458f * a - 0.0638541728f * b);
            var s = Cube(lightness - 0.0894841775f * a - 1.2914855480f * b);

            var linear = new Color(
                4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
                -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
                -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s,
                alpha);

            const float tolerance = 0.0001f;
            var inGamut = linear.r is >= -tolerance and <= 1 + tolerance
                          && linear.g is >= -tolerance and <= 1 + tolerance
                          && linear.b is >= -tolerance and <= 1 + tolerance;

            color = new Color(Mathf.Clamp01(linear.r), Mathf.Clamp01(linear.g), Mathf.Clamp01(linear.b), alpha).gamma;
            return inGamut;
        }

        private static float Cube(float value) => value * value * value;
        private static float Cbrt(float value) => value < 0 ? -Mathf.Pow(-value, 1f / 3f) : Mathf.Pow(value, 1f / 3f);
    }
}
