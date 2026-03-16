// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.UserCenter.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager
    {
        [SerializeField]
        [PropertyOrder(10)]
        [EnhancedBoxGroup("Auth", Color = "@ColorTools.Cyan", SpaceBefore = 8)]
        private int _ApiTimeoutSeconds = 5;
        
        [SerializeField]
        [PropertyOrder(10)]
        [EnhancedBoxGroup("Auth")]
        private int _AuthTimeoutSeconds = 5;

        public static bool IsLoggedIn => AuthStorage.IsValid();

        public event Action<LoginResponse> OnLoginSucceeded;
        public event Action<long, string> OnLoginFailed;
        
        public void Login(string email, string password) { LoginAsync(email, password, this.GetCancellationTokenOnDestroy()).Forget(); }

        public void Logout() => AuthStorage.Clear();
        
        private string AuthLoginUrl()
        {
            var baseUrl = (_ServiceBaseUrl ?? "").TrimEnd('/');
            return $"{baseUrl}/auth/login";
        }

        public void TryLoadStoredToken() { }
        
        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<(bool ok, LoginResponse resp, long status, string message)> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken = GetEffectiveCancellationToken(cancellationToken);

            var url = AuthLoginUrl();
            var payload = new LoginRequest(email, password);
            var body = JsonConvert.SerializeObject(payload);

            var raw = await PostAsync(url, body, false, false, _AuthTimeoutSeconds, cancellationToken);

            if (raw.Success)
            {
                try
                {
                    var jwt = ExtractJwtFromLoginResponse(raw.Raw);
                    if (string.IsNullOrEmpty(jwt))
                    {
                        var msg = "Invalid server response (token not found).";
                        OnLoginFailed?.Invoke(200, msg);
                        return (false, null, 200, msg);
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
                    var expiresIn = expUnix.HasValue ? Mathf.Max(0, (int) (expUnix.Value - now)) : 0;

                    var resp = new LoginResponse(jwt, refreshFromJwt, chosenUserId, expiresIn);

                    AuthStorage.Save(resp.AccessToken, resp.RefreshToken, resp.UserId, expUnix, resp.ExpiresIn);

                    // když login proběhne, server index může být jiný uživatel → invalidate
                    InvalidateIndex();

                    OnLoginSucceeded?.Invoke(resp);
                    return (true, resp, 200, "ok");
                }
                catch (Exception e)
                {
                    var msg = $"Login response parse error: {e.Message}";
                    OnLoginFailed?.Invoke(200, msg);
                    return (false, null, 200, msg);
                }
            }

            var status = raw.StatusCode;
            var msg2 = raw.Error;

            if (status == 401) msg2 = "Incorrect email or password.";
            else if (status == 403) msg2 = "Access denied.";
            else if (status >= 500) msg2 = "Server error. Please try again later.";
            else if (status == 0) msg2 = "Unable to connect (network/TLS/offline).";

            OnLoginFailed?.Invoke(status, msg2);
            return (false, null, status, msg2);
        }
        
        private static string ExtractJwtFromLoginResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            try
            {
                var jo = JToken.Parse(raw);
                return (string) (jo["token"] ?? jo["access_token"] ?? jo["accessToken"]);
            }
            catch
            {
                return null;
            }
        }
    }
}
