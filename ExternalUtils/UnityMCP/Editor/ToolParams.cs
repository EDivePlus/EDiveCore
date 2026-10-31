// Author: František Holubec
// Created: 30.10.2026

using Newtonsoft.Json.Linq;

namespace EDIVE.UnityMCP.Editor
{
    // Tool parameters arrive as PascalCase, camelCase or snake_case depending on the client
    internal static class ToolParams
    {
        public static JToken Get(JObject @params, string pascalName)
        {
            if (@params == null)
                return null;

            var camel = char.ToLowerInvariant(pascalName[0]) + pascalName.Substring(1);
            var snake = System.Text.RegularExpressions.Regex.Replace(pascalName, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
            var token = @params[pascalName] ?? @params[camel] ?? @params[snake];
            return token == null || token.Type == JTokenType.Null ? null : token;
        }

        public static string GetString(JObject @params, string pascalName) => Get(@params, pascalName)?.ToString();

        public static int GetInt(JObject @params, string pascalName, int fallback) => Get(@params, pascalName)?.Value<int>() ?? fallback;

        public static bool GetBool(JObject @params, string pascalName, bool fallback) => Get(@params, pascalName)?.Value<bool>() ?? fallback;
    }
}
