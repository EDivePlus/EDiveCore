using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class MathExtensions
    {
        public static bool Approximately(this float a, float b) => Mathf.Approximately(a, b);
        public static bool IsInRange(this int i, int min, int max) => i >= min && i <= max;
        public static bool IsInRange(this float i, float min, float max) => i >= min && i <= max;
        
        public static int PositiveModulo(this int x, int m)
        {
            var r = x % m;
            return r < 0 ? r + m : r;
        }

        public static float PositiveModulo(this float x, float m)
        {
            var c = x % m;
            if ((c < 0 && m > 0) || (c > 0 && m < 0))
            {
                c += m;
            }

            return c;
        }
        
        public enum RoundingMode
        {
            Floor,
            Nearest,
            Ceil,
            None
        }
         
        public static float Round(this float value, RoundingMode roundingMode = RoundingMode.Nearest)
        {
            return roundingMode switch
            {
                RoundingMode.Floor => Mathf.Floor(value),
                RoundingMode.Nearest => Mathf.Round(value),
                RoundingMode.Ceil => Mathf.Ceil(value),
                RoundingMode.None => value,
                _ => throw new System.ArgumentOutOfRangeException(nameof(roundingMode), roundingMode, null)
            };
        }
        
        public static int RoundToInt(this float value, RoundingMode roundingMode = RoundingMode.Nearest)
        {
            return roundingMode switch
            {
                RoundingMode.Floor => Mathf.FloorToInt(value),
                RoundingMode.Nearest => Mathf.RoundToInt(value),
                RoundingMode.Ceil => Mathf.CeilToInt(value),
                RoundingMode.None => (int) value,
                _ => throw new System.ArgumentOutOfRangeException(nameof(roundingMode), roundingMode, null)
            };
        }
    }
}