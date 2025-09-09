// Author: Radim Holub
// Created: 08.09.2025

using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace EDIVE.Networking.DatabaseManagement
{
    public class AuthService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private string _baseUrl = "https://ediveplus.phil.muni.cz:8443/ediveplus";
        [SerializeField]
        private float _timeoutSeconds = 20f;

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
            var url = $"{_baseUrl}/auth/login";
            var payload = new LoginRequest(email, password);
            var json = JsonConvert.SerializeObject(payload);

            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                var body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = Mathf.CeilToInt(_timeoutSeconds);

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
                            OnLoginFailed?.Invoke(200, "Neplatná odpověď serveru (nenalezen token).");
                            yield break;
                        }
                        
                        var expUnix = JwtUtils.GetUnixExp(jwt);
                        var sub = JwtUtils.GetClaim(jwt, "sub");
                        var emailFromJwt = JwtUtils.GetClaim(jwt, "email");
                        var refreshFromJwt = JwtUtils.GetClaim(jwt, "refresh_token");
                        
                        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var expiresIn = expUnix.HasValue ? (int) Mathf.Max(0, (int) (expUnix.Value - now)) : 0;
                        
                        var resp = new LoginResponse
                        {
                            AccessToken = jwt,
                            RefreshToken = refreshFromJwt,
                            UserId = !string.IsNullOrEmpty(sub) ? sub : emailFromJwt,
                            ExpiresIn = expiresIn
                        };
                        
                        AuthStorage.Save(
                            accessToken: resp.AccessToken,
                            refreshToken: resp.RefreshToken,
                            userId: resp.UserId,
                            expUnixFromJwt: expUnix,
                            expiresInFromApi: resp.ExpiresIn
                        );

                        OnLoginSucceeded?.Invoke(resp);
                    }
                    catch (Exception e)
                    {
                        OnLoginFailed?.Invoke(200, $"Chyba parsování login odpovědi: {e.Message}");
                    }
                }
                else
                {
                    var status = req.responseCode;
                    var msg = req.error;

                    if (status == 401) msg = "Nesprávný e-mail nebo heslo.";
                    else if (status == 403) msg = "Přístup odepřen.";
                    else if (status >= 500) msg = "Chyba serveru. Zkus to prosím později.";
                    else if (status == 0 && req.result == UnityWebRequest.Result.ConnectionError) msg = "Nelze se připojit (síť/TLS).";

                    OnLoginFailed?.Invoke(status, msg);
                }
            }
        }
    }
}
