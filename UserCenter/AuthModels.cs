// Author: Radim Holub
// Created: 08.09.2025

using System;
using Newtonsoft.Json;

namespace EDIVE.UserCenter
{
    [Serializable]
    public class LoginRequest
    {
        [JsonProperty("email")]    public string _Email;
        [JsonProperty("password")] public string _Password;

        public LoginRequest(string email, string password)
        {
            _Email = email;
            _Password = password;
        }
    }

    [Serializable]
    public class LoginResponse
    {
        [JsonProperty("token")]  public string _AccessToken;
        [JsonProperty("refresh_token")] public string _RefreshToken;
        [JsonProperty("userId")]       public string _UserId;
        [JsonProperty("expiresIn")]    public int _ExpiresIn;
    }
}
