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
            return (T[])Enum.GetValues(typeof(T));
        }

        public static T SanitizeFlags<T>(this T value, params T[] validFlags) where T : Enum
        {
            if (validFlags == null || validFlags.Length == 0)
                return value;

            var mask = validFlags.Aggregate<T, ulong>(0, (current, flag) => current | Convert.ToUInt64(flag));
            var clean = Convert.ToUInt64(value) & mask;
            return (T)Enum.ToObject(typeof(T), clean);
        }
    }
}
