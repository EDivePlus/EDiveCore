using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIVE.NativeUtils
{
    public static class EnumUtils
    {
        public static bool IsOneOf<T>(this T value, params T[] comparisons) where T: Enum
        {
            foreach (var comp in comparisons)
            {
                if (Equals(value, comp))
                    return true;
            }

            return false;
        }
        
        public static bool IsOneOf<T>(this T value, T v1) where T: Enum
        {
            return Equals(value, v1);
        }
        
        public static bool IsOneOf<T>(this T value, T v1, T v2) where T: Enum
        {
            return Equals(value, v1) || Equals(value, v2);
        }
        
        public static bool IsOneOf<T>(this T value, T v1, T v2, T v3) where T: Enum
        {
            return Equals(value, v1) || Equals(value, v2) || Equals(value, v3);
        }
        
        public static bool IsOneOf<T>(this T value, T v1, T v2, T v3, T v4) where T: Enum
        {
            return Equals(value, v1) || Equals(value, v2) || Equals(value, v3) || Equals(value, v4);
        }

        public static T Next<T>(this T src) where T : Enum
        {
            var values = (T[])Enum.GetValues(src.GetType());
            var index = (Array.IndexOf(values, src) + 1).PositiveModulo(values.Length);
            return values[index];
        }
        
        public static T Prev<T>(this T src) where T : Enum
        {
            var values = (T[])Enum.GetValues(src.GetType());
            var index = (Array.IndexOf(values, src) - 1).PositiveModulo(values.Length);
            return values[index];
        }

        public static IEnumerable<T> GetValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static T SanitizeFlags<T>(this T value) where T : Enum
        {
            var mask = GetValues<T>()
                .Select(v => Convert.ToUInt64(v))
                .Where(IsSingleBit)
                .Aggregate(0UL, (c, v) => c | v);

            var sanitizedValue = Convert.ToUInt64(value) & mask;
            return (T)Enum.ToObject(typeof(T), sanitizedValue);
        }

        public static bool IsSingleBitFlag<T>(this T value) where T : Enum
        {
            return IsSingleBit(Convert.ToUInt64(value));
        }

        private static bool IsSingleBit(ulong value)
        {
            return value != 0 && (value & (value - 1)) == 0;
        }
    }
}
