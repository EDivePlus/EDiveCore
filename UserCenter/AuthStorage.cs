// Author: Radim Holub
// Created: 08.09.2025

using System;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public static class AuthStorage
    {
        private const string K_ACCESS    = "auth.access";
        private const string K_REFRESH   = "auth.refresh";
        private const string K_USERID    = "auth.userId";
        private const string K_EXPIRESAT = "auth.expiresAt";
        private const string K_EMAIL = "auth.lastEmail";

        public static void Save(string accessToken, string refreshToken, string userId, long? expUnixFromJwt, int expiresInFromApi)
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
            PlayerPrefs.SetString(K_USERID, userId ?? "");
            PlayerPrefs.SetString(K_EXPIRESAT, expiresAtUnix.ToString());
            PlayerPrefs.Save();
        }

        public static string GetAccessToken()  => PlayerPrefs.GetString(K_ACCESS, "");
        public static string GetRefreshToken() => PlayerPrefs.GetString(K_REFRESH, "");
        public static string GetUserId()       => PlayerPrefs.GetString(K_USERID, "");

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
            PlayerPrefs.DeleteKey(K_USERID);
            PlayerPrefs.DeleteKey(K_EXPIRESAT);
        }
        public static void SetLastEmail(string email)
        {
            PlayerPrefs.SetString(K_EMAIL, email ?? "");
            PlayerPrefs.Save();
        }

        public static string GetLastEmail()
        {
            return PlayerPrefs.GetString(K_EMAIL, "");
        }
    }
}