// Author: Radim Holub
// Created: 08.09.2025
using System;
using Newtonsoft.Json;

namespace EDIVE.Networking.DatabaseManagement
{
    [Serializable]
    public class LoginRequest
    {
        [JsonProperty("email")]    public string Email;
        [JsonProperty("password")] public string Password;

        public LoginRequest(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    [Serializable]
    public class LoginResponse
    {
        [JsonProperty("token")]  public string AccessToken;
        [JsonProperty("refresh_token")] public string RefreshToken;
        [JsonProperty("userId")]       public string UserId;
        [JsonProperty("expiresIn")]    public int ExpiresIn;
    }
}
