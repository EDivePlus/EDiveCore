// Author: Radim Holub
// Created: 08.09.2025
using System;
using UnityEngine;

namespace EDIVE.Networking.DatabaseManagement
{
    [CreateAssetMenu(fileName = "TokenStore", menuName = "EDIVE/Auth/Token Store")]
    public class TokenStore : ScriptableObject
    {
        [SerializeField] private string _accessToken;
        [SerializeField] private string _refreshToken;
        [SerializeField] private string _userId;
        [SerializeField] private long   _expiresAtUnix;

        public string AccessToken => _accessToken;
        public string RefreshToken => _refreshToken;
        public string UserId       => _userId;
        public DateTime ExpiresAt  => DateTimeOffset.FromUnixTimeSeconds(_expiresAtUnix).UtcDateTime;

        public bool IsValid => !string.IsNullOrEmpty(_accessToken)
                             && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < _expiresAtUnix;

        public void Save(LoginResponse r)
        {
            _accessToken  = r.AccessToken;
            _refreshToken = r.RefreshToken;
            _userId       = r.UserId;

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Mathf.Max(0, r.ExpiresIn));
            _expiresAtUnix = expiresAt.ToUnixTimeSeconds();

            PlayerPrefs.SetString("auth.access", _accessToken ?? "");
            PlayerPrefs.SetString("auth.refresh", _refreshToken ?? "");
            PlayerPrefs.SetString("auth.userId", _userId ?? "");
            PlayerPrefs.SetString("auth.expiresAt", _expiresAtUnix.ToString());
            PlayerPrefs.Save();
        }

        public void LoadFromPrefsIfEmpty()
        {
            if (!string.IsNullOrEmpty(_accessToken)) return;
            _accessToken  = PlayerPrefs.GetString("auth.access", "");
            _refreshToken = PlayerPrefs.GetString("auth.refresh", "");
            _userId       = PlayerPrefs.GetString("auth.userId", "");
            if (long.TryParse(PlayerPrefs.GetString("auth.expiresAt", "0"), out var ts))
                _expiresAtUnix = ts;
        }

        public void Clear()
        {
            _accessToken = _refreshToken = _userId = "";
            _expiresAtUnix = 0;
            PlayerPrefs.DeleteKey("auth.access");
            PlayerPrefs.DeleteKey("auth.refresh");
            PlayerPrefs.DeleteKey("auth.userId");
            PlayerPrefs.DeleteKey("auth.expiresAt");
        }
    }
}
