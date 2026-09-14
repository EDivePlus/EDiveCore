// Author: František Holubec
// Created: 15.09.2026

using System.Text;

namespace EDIVE.CredentialStore
{
    public static class CredentialUtils
    {
        public const int MAX_NAME_LENGTH = 256;

        // Windows caps a credential blob at 2560 bytes, written as UTF-16.
        public const int MAX_SECRET_LENGTH = 1280;

        public const char SERVICE_SEPARATOR = ':';
        public const string NAME_PUNCTUATION = ".-_+@/";

        public static bool TryNormalizeService(string value, out string result, out string error) => TryNormalizeName(value, true, out result, out error);

        public static bool TryNormalizeAccount(string value, out string result, out string error) => TryNormalizeName(value, false, out result, out error);

        public static string SanitizeService(string value, char replacement = '-') => SanitizeName(value, true, replacement);

        public static string SanitizeAccount(string value, char replacement = '-') => SanitizeName(value, false, replacement);

        internal static CredentialResult ValidateNames(ref string service, ref string account)
        {
            var result = ValidateName(ref service, "Service", true);
            return result ? ValidateName(ref account, "Account", false) : result;
        }

        internal static CredentialResult ValidateNamesAndSecret(ref string service, ref string account, string secret)
        {
            var result = ValidateNames(ref service, ref account);
            if (!result)
                return result;

            if (secret == null)
                return CredentialResult.Invalid("Secret null.");

            return secret.Length > MAX_SECRET_LENGTH
                ? CredentialResult.Invalid($"Secret is longer than {MAX_SECRET_LENGTH} characters.")
                : CredentialResult.Ok;
        }

        private static bool TryNormalizeName(string value, bool isService, out string result, out string error)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "is empty";
                return false;
            }

            // Windows target names are case-insensitive while macOS and Linux attributes are not,
            // so everything is lowercased to make the three agree.
            var normalized = value.Trim().ToLowerInvariant();
            if (normalized.Length > MAX_NAME_LENGTH)
            {
                error = $"is longer than {MAX_NAME_LENGTH} characters";
                return false;
            }

            foreach (var c in normalized)
            {
                if (IsAllowedNameChar(c, isService))
                    continue;

                error = DescribeInvalidChar(c, isService);
                return false;
            }

            result = normalized;
            error = null;
            return true;
        }

        private static string SanitizeName(string value, bool isService, char replacement)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!IsAllowedNameChar(replacement, isService))
                replacement = '-';

            var normalized = value.Trim().ToLowerInvariant();
            var builder = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
                builder.Append(IsAllowedNameChar(c, isService) ? c : replacement);

            var sanitized = builder.ToString();
            if (sanitized.Length > MAX_NAME_LENGTH)
                sanitized = sanitized[..MAX_NAME_LENGTH];

            return TryNormalizeName(sanitized, isService, out var result, out _) ? result : null;
        }

        // Deliberately ASCII only: CredWrite rejects wildcards, and the wider ecosystem mangles
        // non-ASCII attribute names. A service cannot hold the separator that splits it from the account.
        private static bool IsAllowedNameChar(char c, bool isService)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                return true;

            return c == SERVICE_SEPARATOR ? !isService : NAME_PUNCTUATION.IndexOf(c) >= 0;
        }

        private static CredentialResult ValidateName(ref string value, string label, bool isService)
        {
            if (!TryNormalizeName(value, isService, out var normalized, out var error))
                return CredentialResult.Invalid($"{label} {error}.");

            value = normalized;
            return CredentialResult.Ok;
        }

        private static string DescribeInvalidChar(char c, bool isService)
        {
            if (c == SERVICE_SEPARATOR)
                return $"contains '{SERVICE_SEPARATOR}', which separates the service from the account";
            if (c > sbyte.MaxValue)
                return $"contains a non-ASCII character (U+{(int) c:X4})";
            if (char.IsControl(c))
                return $"contains a control character (U+{(int) c:X4})";

            var allowed = isService ? NAME_PUNCTUATION : NAME_PUNCTUATION + SERVICE_SEPARATOR;
            return $"contains '{c}', allowed are a-z 0-9 and {allowed}";
        }
    }
}
