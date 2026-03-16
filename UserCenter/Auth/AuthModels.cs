// Author: Radim Holub
// Created: 08.09.2025

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter.Auth
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoginRequest
    {
        [JsonProperty("email")]
        [SerializeField]
        private string _Email;
        
        [JsonProperty("password")]
        [SerializeField]
        private string _Password;

        public LoginRequest(string email, string password)
        {
            _Email = email;
            _Password = password;
        }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoginResponse
    {
        [JsonProperty("token")]
        [SerializeField]
        private string _AccessToken;
        
        [JsonProperty("refresh_token")]
        [SerializeField]
        private string _RefreshToken;
        
        [JsonProperty("userId")]
        [SerializeField]
        private string _UserId;
        
        [JsonProperty("expiresIn")]
        [SerializeField]
        private int _ExpiresIn;
        
        public string AccessToken => _AccessToken;
        public string RefreshToken => _RefreshToken;
        public string UserId => _UserId;
        public int ExpiresIn => _ExpiresIn;
        
        public LoginResponse(string accessToken, string refreshToken, string userId, int expiresIn)
        {
            _AccessToken = accessToken;
            _RefreshToken = refreshToken;
            _UserId = userId;
            _ExpiresIn = expiresIn;
        }
    }
}
