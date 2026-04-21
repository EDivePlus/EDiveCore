// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.UserCenter.Auth;
using EDIVE.UserCenter.SaveData;
using Newtonsoft.Json;
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

        public void Login(string email, string password) => LoginAsync(email, password, this.GetCancellationTokenOnDestroy()).Forget();

        public void AnonymousLogin() => AnonymousLoginAsync(this.GetCancellationTokenOnDestroy()).Forget();

        public void Logout() => LogoutAsync(this.GetCancellationTokenOnDestroy()).Forget();

        public async UniTask LogoutAsync(CancellationToken cancellationToken = default)
        {
            var effectiveToken = cancellationToken == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : cancellationToken;

            await FlushAllDirtyEntries(effectiveToken);
            AuthStorage.Clear();
            _local = new PlayerPrefsSaveDataStore();
        }
        
        private string AuthLoginUrl => $"{ServiceBaseUrl}/auth/login";
        private string AnonymousAuthLoginUrl => $"{ServiceBaseUrl}/auth/anonymous-login";
        private string AuthMeUrl => $"{ServiceBaseUrl}/auth/me";

        /// <summary>
        /// Calls /auth/me to verify the stored token is still valid server-side.
        /// If the server rejects it, the user is logged out locally.
        /// </summary>
        [Button]
        [PropertyOrder(98)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<bool> CheckAuthAsync(CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.IsValid())
                return false;

            var accessToken = AuthStorage.GetAccessToken();

            var response = await RestUtils.GetAsync<ApiResponse<AuthUserInfo>>(
                AuthMeUrl,
                authToken: accessToken,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccess && response.Result?.Status == 0 && response.Result?.Data != null)
                return true;

            Debug.LogWarning("[UserCenter] Auth check failed — token is no longer valid. Logging out.");
            await LogoutAsync(cancellationToken);
            return false;
        }

        public void TryLoadStoredToken()
        {
            if (!IsLoggedIn) return;
            var userId = AuthStorage.GetUserId();
            if (!string.IsNullOrEmpty(userId))
                _local = new PlayerPrefsSaveDataStore($"uc.savedata.{userId}.");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<NetworkResponse<LoginResponse>> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default
        )
        {
            var request = new LoginRequest(_AppSecret, email, password);

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, LoginRequest>(
                AuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleLoginResponse(response);
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<NetworkResponse<LoginResponse>> AnonymousLoginAsync(
            CancellationToken cancellationToken = default
        )
        {
            var token = GetOrCreateAnonymousToken();
            var request = new AnonymousLoginRequest(_AppSecret, token);

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, AnonymousLoginRequest>(
                AnonymousAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleLoginResponse(response);
        }

        private const string K_ANONYMOUS_TOKEN = "auth.anonymousToken";

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

        private NetworkResponse<LoginResponse> HandleLoginResponse(NetworkResponse<ApiResponse<LoginResponse>> response)
        {
            // Network-level failure (timeout, DNS, etc.)
            if (!response.IsSuccess && response.Result is null)
            {
                Debug.LogError($"[UserCenter] Login request failed: {response.ErrorMessage}");
                OnLoginFailed?.Invoke(response.StatusCode, response.ErrorMessage);
                return NetworkResponse<LoginResponse>.Error(response.StatusCode, response.ErrorMessage);
            }

            var apiResponse = response.Result;

            // API-level failure (invalid credentials, missing argument, etc.)
            if (apiResponse == null || apiResponse.Status != 0 || apiResponse.Data == null)
            {
                var message = apiResponse?.Message ?? "Unknown error";
                var statusCode = apiResponse?.Status ?? -1;
                Debug.LogError($"[UserCenter] Login failed ({statusCode}): {message}");
                OnLoginFailed?.Invoke(statusCode, message);
                return NetworkResponse<LoginResponse>.Error(response.StatusCode, message);
            }

            var loginResponse = apiResponse.Data;
            var accessToken = loginResponse.AccessToken;
            var userInfo = loginResponse.AuthUser;

            // Extract expiration from the JWT itself, fall back to expires_in
            var expUnix = JwtUtils.GetUnixExp(accessToken);
            AuthStorage.Save(accessToken, null, expUnix, loginResponse.ExpiresIn, userInfo);
            var userId = userInfo?.Id ?? JwtUtils.GetClaim(accessToken, "sub") ?? "";
            _local = new PlayerPrefsSaveDataStore($"uc.savedata.{userId}.");

            var email = userInfo?.Email ?? JwtUtils.GetClaim(accessToken, "email") ?? "";
            if (!string.IsNullOrEmpty(email))
                AuthStorage.SetLastEmail(email);

            Debug.Log("[UserCenter] Login successful.");
            OnLoginSucceeded?.Invoke(loginResponse);

            var result = NetworkResponse<LoginResponse>.Success(response.StatusCode, loginResponse);
            Debug.Log($"[UserCenter] Login response (formatted): {JsonConvert.SerializeObject(result, Formatting.Indented)}");
            return result;
        }
    }
}
