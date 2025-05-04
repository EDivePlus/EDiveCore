using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class MathExtensions
    {
        public static float Remap(this int input, float inputMin, float inputMax, float targetMin, float targetMax)
        {
            return targetMin + (input - inputMin) * (targetMax - targetMin) / (inputMax - inputMin);

        }
        public static float Remap(this float input, float inputMin, float inputMax, float targetMin, float targetMax)
        {
            return targetMin + (input - inputMin) * (targetMax - targetMin) / (inputMax - inputMin);
        }

        public static double Remap(this double input, double inputMin, double inputMax, double targetMin, double targetMax)
        {
            return targetMin + (input - inputMin) * (targetMax - targetMin) / (inputMax - inputMin);
        }

        public static bool IsPowerOfTwo(this int value)
        {
            return (value & (value - 1)) == 0;
        }

        public static bool FastApproximately(this float a, float b)
        {
            return FastApproximately(a, b, Mathf.Epsilon);
        }

        public static bool FastApproximately(this float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        public static int CeilToPowerOfTwo(this float x)
        {
            return Mathf.CeilToInt(x).CeilToPowerOfTwo();
        }

        public static int FloorToPowerOfTwo(this float x)
        {
            return Mathf.FloorToInt(x).FloorToPowerOfTwo();
        }

        public static int CeilToPowerOfTwo(this int x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;

            return x;
        }

        public static int FloorToPowerOfTwo(this int x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;

            return x ^ (x >> 1);
        }

        public static bool Approximately(this float a, float b) => Mathf.Approximately(a, b);
        public static bool IsInRange(this int i, int min, int max) => i >= min && i <= max;
        public static bool IsInRange(this float input, float a, float b, float bias = 0)
        {
            return a < b ? input >= a - bias && input <= b + bias : input >= b - bias && input <= a + bias;
        }

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