// Author: Radim Holub
// Created: 08.09.2025

using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.UserCenter
{
    public class AuthService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private string _BaseUrl = "https://ediveplus.phil.muni.cz:8443/ediveplus";
        
        [SerializeField]
        private float _TimeoutSeconds = 20f;

        public bool IsLoggedIn => AuthStorage.IsValid();

        public event Action<LoginResponse> OnLoginSucceeded;
        public event Action<long, string> OnLoginFailed;

        public void TryLoadStoredToken() { }

        public void Login(string email, string password)
        {
            StopAllCoroutines();
            StartCoroutine(LoginCoroutine(email, password));
        }

        private IEnumerator LoginCoroutine(string email, string password)
        {
            var url = $"{_BaseUrl}/auth/login";
            var payload = new LoginRequest(email, password);
            var json = JsonConvert.SerializeObject(payload);

            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = Mathf.CeilToInt(_TimeoutSeconds);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var raw = req.downloadHandler.text ?? "";
                        
                        string jwt = null;
                        try
                        {
                            var jo = JToken.Parse(raw);
                            jwt = (string) (jo["token"] ?? jo["access_token"] ?? jo["accessToken"]);
                        }
                        catch
                        {
                        }
                        

                        if (string.IsNullOrEmpty(jwt))
                        {
                            OnLoginFailed?.Invoke(200, "Invalid server response (token not found).");
                            yield break;
                        }
                        
                        var expUnix = JwtUtils.GetUnixExp(jwt);
                        var sub = JwtUtils.GetClaim(jwt, "sub");
                        var emailFromJwt = JwtUtils.GetClaim(jwt, "email");
                        var refreshFromJwt = JwtUtils.GetClaim(jwt, "refresh_token");
                        
                        var userUuid = JwtUtils.GetClaim(jwt, "uuid") ?? JwtUtils.GetClaim(jwt, "userUuid");
                        var chosenUserId = !string.IsNullOrEmpty(userUuid)
                            ? userUuid
                            : (!string.IsNullOrEmpty(sub) ? sub : emailFromJwt);
                        
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var expiresIn = expUnix.HasValue ? (int) Mathf.Max(0, (int) (expUnix.Value - now)) : 0;
                        
                        var resp = new LoginResponse
                        {
                            _AccessToken = jwt,
                            _RefreshToken = refreshFromJwt,
                            _UserId = chosenUserId,
                            _ExpiresIn = expiresIn
                        };
                        
                        AuthStorage.Save(
                            accessToken: resp._AccessToken,
                            refreshToken: resp._RefreshToken,
                            userId: resp._UserId,
                            expUnixFromJwt: expUnix,
                            expiresInFromApi: resp._ExpiresIn
                        );

                        OnLoginSucceeded?.Invoke(resp);
                    }
                    catch (Exception e)
                    {
                        OnLoginFailed?.Invoke(200, $"Login response parse error: {e.Message}");
                    }
                }
                else
                {
                    var status = req.responseCode;
                    var msg = req.error;

                    if (status == 401) msg = "Incorrect email or password.";
                    else if (status == 403) msg = "Access denied.";
                    else if (status >= 500) msg = "Server error. Please try again later.";
                    else if (status == 0 && req.result == UnityWebRequest.Result.ConnectionError) msg = "Unable to connect (network/TLS).";

                    OnLoginFailed?.Invoke(status, msg);
                }
            }
        }
        public void Logout()
        {
            AuthStorage.Clear();
        }
        
    }
}
