// Author: Radim Holub
// Created: 08.09.2025

using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EDIVE.UserCenter
{
    public static class JwtUtils
    {
        public static JObject DecodePayloadToJson(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                throw new ArgumentException("JWT is null or empty");

            var parts = jwt.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException($"Invalid JWT, expected 3 parts, got {parts.Length}.");

            var payloadB64Url = parts[1];
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payloadB64Url));
            return JObject.Parse(json);
        }

        public static long? GetUnixExp(string jwt)
        {
            try
            {
                var jo = DecodePayloadToJson(jwt);
                var expTok = jo["exp"];
                if (expTok == null) return null;
                return long.TryParse(expTok.ToString(), out var exp) ? exp : (long?)null;
            }
            catch
            {
                return null;
            }
        }

        public static string GetClaim(string jwt, string claimName)
        {
            if (string.IsNullOrEmpty(jwt) || string.IsNullOrEmpty(claimName))
                return null;

            try
            {
                var jo = DecodePayloadToJson(jwt);
                return jo[claimName]?.ToString();
            }
            catch
            {
                return null;
            }
        }
        
        public static T DecodePayload<T>(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                throw new ArgumentException("JWT is null/empty");

            var parts = jwt.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException($"Invalid JWT (expected 3 parts, got {parts.Length}).");

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(payloadJson);
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            return Convert.FromBase64String(output);
        }
    }
}
