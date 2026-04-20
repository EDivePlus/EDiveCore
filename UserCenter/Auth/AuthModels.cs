// Author: Radim Holub
// Created: 08.09.2025

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter.Auth
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoginRequest
    {
        [JsonProperty("app_secret")]
        [SerializeField]
        private string _AppSecret;
        
        [JsonProperty("email")]
        [SerializeField]
        private string _Email;

        [JsonProperty("password")]
        [SerializeField]
        private string _Password;

        public LoginRequest(string appSecret, string email, string password)
        {
            _AppSecret = appSecret;
            _Email = email;
            _Password = password;
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class AnonymousLoginRequest
    {
        [JsonProperty("app_secret")]
        [SerializeField]
        private string _AppSecret;
        
        [JsonProperty("token")]
        [SerializeField]
        private string _Token;

        public AnonymousLoginRequest(string appSecret, string token)
        {
            _AppSecret = appSecret;
            _Token = token;
        }
    }

    /// <summary>
    /// Matches the backend AuthTokenResponse.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoginResponse
    {
        [JsonProperty("access_token")]
        [SerializeField]
        private string _AccessToken;

        [JsonProperty("token_type")]
        [SerializeField]
        private string _TokenType;

        [JsonProperty("expires_in")]
        [SerializeField]
        private int _ExpiresIn;

        [JsonProperty("app_roles")]
        [SerializeField]
        private List<string> _AppRoles;

        [JsonProperty("app_secret")]
        [SerializeField]
        private string _AppSecret;

        public string AccessToken => _AccessToken;
        public string TokenType => _TokenType;
        public int ExpiresIn => _ExpiresIn;
        public List<string> AppRoles => _AppRoles;
        public string AppSecret => _AppSecret;
    }

    /// <summary>
    /// Matches the backend UserInfoResponse from /auth/me.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class MeResponse
    {
        [JsonProperty("id")]
        [SerializeField]
        private string _Id;

        [JsonProperty("email")]
        [SerializeField]
        private string _Email;

        [JsonProperty("name")]
        [SerializeField]
        private string _Name;

        [JsonProperty("roles")]
        [SerializeField]
        private List<string> _Roles;
        
        [JsonProperty("is_anonymous")]
        [SerializeField]
        private bool _IsAnonymous;

        public string Id => _Id;
        public string Email => _Email;
        public string Name => _Name;
        public List<string> Roles => _Roles;
        public bool IsAnonymous => _IsAnonymous;
    }
}
