// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    public class ClientAuthService : AAuthService<LoginResponse>
    {
        private const string K_ANONYMOUS_TOKEN = "auth.anonymousToken";

        protected override AuthStorage Storage => AuthStorage.Client;
        protected override string LogScope => "Login";

        private string ClientAuthLoginUrl => $"{Settings.ServiceBaseUrl}/auth/login";
        private string ClientAnonymousAuthLoginUrl => $"{Settings.ServiceBaseUrl}/auth/anonymous-login";
        private string ClientAuthMeUrl => $"{Settings.ServiceBaseUrl}/auth/me";

        public static string GetUserId()
        {
            return AuthStorage.Client.GetInfo<AuthUserInfo>()?.Id
                   ?? JwtUtils.GetClaim(AuthStorage.Client.GetAccessToken(), "sub")
                   ?? "";
        }

        public void LoginClient(string email, string password) =>
            LoginClientAsync(email, password, destroyCancellationToken).Forget();

        public void AnonymousLoginClient() =>
            AnonymousLoginClientAsync(destroyCancellationToken).Forget();

        public void LogoutClient() =>
            LogoutAsync(destroyCancellationToken).Forget();

        public UniTask LogoutClientAsync(CancellationToken cancellationToken = default) =>
            LogoutAsync(cancellationToken);

        public void TryLoadStoredClientToken()
        {
            // No-op: SaveDataService seeds its per-user store from AuthStorage at boot and on
            // OnLoginSucceeded. Kept for compatibility with callers that still invoke this.
        }

        [Button]
        [PropertyOrder(98)]
        [EnhancedBoxGroup("Auth", Color = "@ColorTools.Cyan", SpaceBefore = 8)]
        public async UniTask<bool> CheckClientAuthAsync(CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return false;

            var accessToken = AuthStorage.Client.GetAccessToken();

            var response = await RestUtils.GetAsync<ApiResponse<AuthUserInfo>>(
                ClientAuthMeUrl,
                authToken: accessToken,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccess && response.Result?.Status == 0 && response.Result?.Data != null)
                return true;

            Debug.LogWarning("[ServiceHub] Auth check failed — token is no longer valid. Logging out.");
            await LogoutAsync(cancellationToken);
            return false;
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<NetworkResponse<LoginResponse>> LoginClientAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var request = new LoginRequest(Settings.AppSecret, email, password);

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, LoginRequest>(
                ClientAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleLoginResponse(response);
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<NetworkResponse<LoginResponse>> AnonymousLoginClientAsync(
            CancellationToken cancellationToken = default)
        {
            var token = GetOrCreateAnonymousToken();
            var request = new AnonymousLoginRequest(Settings.AppSecret, token);

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, AnonymousLoginRequest>(
                ClientAnonymousAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleLoginResponse(response);
        }

        protected override void SaveLoginToStorage(LoginResponse data)
        {
            var accessToken = data.AccessToken;
            var userInfo = data.AuthUser;
            var expUnix = JwtUtils.GetUnixExp(accessToken);
            var infoJson = userInfo != null ? JsonConvert.SerializeObject(userInfo) : null;

            AuthStorage.Client.Save(accessToken, null, expUnix, data.ExpiresIn, infoJson);

            var email = userInfo?.Email ?? JwtUtils.GetClaim(accessToken, "email") ?? "";
            if (!string.IsNullOrEmpty(email))
                AuthStorage.Client.SetLastEmail(email);
        }

        private static string GetOrCreateAnonymousToken()
        {
            var existing = PlayerPrefs.GetString(K_ANONYMOUS_TOKEN, "");
            if (!string.IsNullOrEmpty(existing))
                return existing;

            var token = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(K_ANONYMOUS_TOKEN, token);
            PlayerPrefs.Save();
            return token;
        }
    }
}
