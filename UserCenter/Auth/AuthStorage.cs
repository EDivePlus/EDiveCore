// Author: Radim Holub
// Created: 08.09.2025

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter.Auth
{
    public static class AuthStorage
    {
        private const string K_ACCESS = "auth.access";
        private const string K_REFRESH = "auth.refresh";
        private const string K_EXPIRESAT = "auth.expiresAt";
        private const string K_EMAIL = "auth.lastEmail";
        private const string K_USERINFO = "auth.userInfo";

        public static void Save(string accessToken, string refreshToken, long? expUnixFromJwt, int expiresInFromApi, AuthUserInfo authUserInfo = null)
        {
            long expiresAtUnix;
            if (expUnixFromJwt.HasValue)
            {
                expiresAtUnix = expUnixFromJwt.Value;
            }
            else
            {
                var dt = DateTimeOffset.UtcNow.AddSeconds(Mathf.Max(0, expiresInFromApi));
                expiresAtUnix = dt.ToUnixTimeSeconds();
            }

            PlayerPrefs.SetString(K_ACCESS, accessToken ?? "");
            PlayerPrefs.SetString(K_REFRESH, refreshToken ?? "");
            PlayerPrefs.SetString(K_EXPIRESAT, expiresAtUnix.ToString());

            if (authUserInfo != null)
                PlayerPrefs.SetString(K_USERINFO, JsonConvert.SerializeObject(authUserInfo));

            PlayerPrefs.Save();
        }

        public static string GetAccessToken() => PlayerPrefs.GetString(K_ACCESS, "");
        public static string GetRefreshToken() => PlayerPrefs.GetString(K_REFRESH, "");
        public static string GetUserId() => GetUserInfo()?.Id ?? "";

        public static AuthUserInfo GetUserInfo()
        {
            var json = PlayerPrefs.GetString(K_USERINFO, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<AuthUserInfo>(json); }
            catch { return null; }
        }

        public static long GetExpiresAtUnix()
        {
            var s = PlayerPrefs.GetString(K_EXPIRESAT, "0");
            return long.TryParse(s, out var v) ? v : 0;
        }

        public static bool IsValid()
        {
            var token = GetAccessToken();
            if (string.IsNullOrEmpty(token)) return false;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now < GetExpiresAtUnix();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(K_ACCESS);
            PlayerPrefs.DeleteKey(K_REFRESH);
            PlayerPrefs.DeleteKey(K_EXPIRESAT);
            PlayerPrefs.DeleteKey(K_EMAIL);
            PlayerPrefs.DeleteKey(K_USERINFO);
            PlayerPrefs.Save();
        }

        public static void SetLastEmail(string email)
        {
            PlayerPrefs.SetString(K_EMAIL, email ?? "");
            PlayerPrefs.Save();
        }

        public static string GetLastEmail() { return PlayerPrefs.GetString(K_EMAIL, ""); }
    }
}
