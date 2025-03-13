using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EDIVE.NativeUtils
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (source == null) return false;
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static string Nicify(this string original)
        {
            var camel = Regex.Replace(original, "(\\B[A-Z])", " $1");
            var snake = camel.Replace("_", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(snake);
        }

        public static string Truncate(this string value, int maxLength, bool addDotsToEnd = false)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.Length <= maxLength)
                return value;

            var truncated = value[..maxLength];
            return addDotsToEnd ? $"{truncated}..." : truncated;
        }
    }
}
