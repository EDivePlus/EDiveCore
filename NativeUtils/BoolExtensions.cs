using System.Text.RegularExpressions;

namespace EDIVE.NativeUtils
{
    public static class BoolExtensions
    {
        private static readonly Regex TRUE_PATTERN = new("^(1|true|t|yes|y|on)$", RegexOptions.IgnoreCase);
        private static readonly Regex FALSE_PATTERN = new("^(0|false|f|no|n|off|)$", RegexOptions.IgnoreCase);

        public static bool ParseBool(this string str)
        {
            if (TRUE_PATTERN.IsMatch(str))
                return true;
            if (FALSE_PATTERN.IsMatch(str))
                return false;
            throw new System.FormatException($"ConfigValue '{str}' is not a boolean value");
        }

        public static bool TryParseBool(this string str, out bool result)
        {
            if (TRUE_PATTERN.IsMatch(str))
            {
                result = true;
                return true;
            }

            if (FALSE_PATTERN.IsMatch(str))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }
    }
}
