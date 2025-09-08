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
        [SerializeField]
        private TokenStore _tokenStore;

        public bool IsLoggedIn => _tokenStore != null && _tokenStore.IsValid;

        public event Action<LoginResponse> OnLoginSucceeded;
        public event Action<long, string> OnLoginFailed;

        public void TryLoadStoredToken() => _tokenStore?.LoadFromPrefsIfEmpty();

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

                // Unity 2022+: request.result je spolehlivé
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<LoginResponse>(req.downloadHandler.text);
                        if (resp == null || string.IsNullOrEmpty(resp.AccessToken))
                        {
                            OnLoginFailed?.Invoke(200, "Neplatná odpověď serveru (chybí access token).");
                            yield break;
                        }

                        _tokenStore?.Save(resp);
                        OnLoginSucceeded?.Invoke(resp);
                    }
                    catch (Exception e)
                    {
                        OnLoginFailed?.Invoke(200, $"Chyba parsování JSON: {e.Message}");
                    }
                }
                else
                {
                    var status = req.responseCode; // 0 pokud timeout/DNS/TLS
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
