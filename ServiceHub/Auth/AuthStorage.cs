// Author: Radim Holub
// Created: 08.09.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    public class AuthStorage
    {
        private const int REFRESH_MARGIN_SECONDS = 60;

        private readonly string _kAccess;
        private readonly string _kRefresh;
        private readonly string _kExpiresAt;
        private readonly string _kInfo;
        private readonly string _kLastEmail;

        private static AuthStorage _client;
        private static AuthStorage _server;

        public static AuthStorage Client => _client ??= new AuthStorage("auth.");
        public static AuthStorage Server => _server ??= new AuthStorage("server.auth.");

        public AuthStorage(string prefix)
        {
            var p = prefix ?? "";
            _kAccess = p + "access";
            _kRefresh = p + "refresh";
            _kExpiresAt = p + "expiresAt";
            _kInfo = p + "info";
            _kLastEmail = p + "lastEmail";
        }

        // Renews the access token, set by the auth service that owns the refresh token
        public Func<UniTask<bool>> Refresher { get; set; }

        // Saved or cleared tokens
        public event Action SessionChanged;

        private bool CanRefresh => Refresher != null && !string.IsNullOrEmpty(GetRefreshToken());

        // Signed in, the access token may need a refresh first
        public bool HasSession => IsValid() || CanRefresh;

        // Valid access token, refreshed when it expires within a minute. Null when signed out
        public async UniTask<string> GetAccessTokenAsync()
        {
            var expiring = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + REFRESH_MARGIN_SECONDS >= GetExpiresAtUnix();
            if (expiring && CanRefresh)
                await Refresher();
            return IsValid() ? GetAccessToken() : null;
        }

        // Sends with a valid token, on 401 refreshes and retries once
        public async UniTask<NetworkResponse<T>> SendAsync<T>(Func<string, UniTask<NetworkResponse<T>>> send)
        {
            var token = await GetAccessTokenAsync();
            if (token == null)
                return NetworkResponse<T>.Error(401, "Not authenticated");

            var response = await send(token);
            if (response.StatusCode != 401 || !CanRefresh || !await Refresher())
                return response;
            return await send(GetAccessToken());
        }

        public void Save(string accessToken, string refreshToken, long? expUnixFromJwt, int expiresInFromApi, string infoJson = null)
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

            PlayerPrefs.SetString(_kAccess, accessToken ?? "");
            PlayerPrefs.SetString(_kRefresh, refreshToken ?? "");
            PlayerPrefs.SetString(_kExpiresAt, expiresAtUnix.ToString());

            if (infoJson != null)
                PlayerPrefs.SetString(_kInfo, infoJson);

            PlayerPrefs.Save();
            SessionChanged?.Invoke();
        }

        public string GetAccessToken() => PlayerPrefs.GetString(_kAccess, "");
        public string GetRefreshToken() => PlayerPrefs.GetString(_kRefresh, "");

        public string GetInfoJson() => PlayerPrefs.GetString(_kInfo, "");

        public void SetInfoJson(string infoJson)
        {
            PlayerPrefs.SetString(_kInfo, infoJson ?? "");
            PlayerPrefs.Save();
        }

        public T GetInfo<T>() where T : class
        {
            var json = GetInfoJson();
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<T>(json); }
            catch { return null; }
        }

        public long GetExpiresAtUnix()
        {
            var s = PlayerPrefs.GetString(_kExpiresAt, "0");
            return long.TryParse(s, out var v) ? v : 0;
        }

        public bool IsValid()
        {
            var token = GetAccessToken();
            if (string.IsNullOrEmpty(token)) return false;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now < GetExpiresAtUnix();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_kAccess);
            PlayerPrefs.DeleteKey(_kRefresh);
            PlayerPrefs.DeleteKey(_kExpiresAt);
            PlayerPrefs.DeleteKey(_kLastEmail);
            PlayerPrefs.DeleteKey(_kInfo);
            PlayerPrefs.Save();
            SessionChanged?.Invoke();
        }

        public void SetLastEmail(string email)
        {
            PlayerPrefs.SetString(_kLastEmail, email ?? "");
            PlayerPrefs.Save();
        }

        public string GetLastEmail() => PlayerPrefs.GetString(_kLastEmail, "");
    }
}
