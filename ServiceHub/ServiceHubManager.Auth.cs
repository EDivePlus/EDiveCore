// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Http;
using EDIVE.Networking;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.SaveData;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    public partial class ServiceHubManager
    {
        [SerializeField]
        [PropertyOrder(10)]
        [EnhancedBoxGroup("Auth", Color = "@ColorTools.Cyan", SpaceBefore = 8)]
        private int _ApiTimeoutSeconds = 5;
        
        [SerializeField]
        [PropertyOrder(10)]
        [EnhancedBoxGroup("Auth")]
        private int _AuthTimeoutSeconds = 5;

        public static bool IsLoggedIn => AuthStorage.Client.IsValid();

        public event Action<LoginResponse> OnClientLoginSucceeded;
        public event Action<long, string> OnClientLoginFailed;
        
        private string ClientAuthLoginUrl => $"{ServiceBaseUrl}/auth/login";
        private string ClientAnonymousAuthLoginUrl => $"{ServiceBaseUrl}/auth/anonymous-login";
        private string ClientAuthMeUrl => $"{ServiceBaseUrl}/auth/me";
        
        public void LoginClient(string email, string password) => LoginClientAsync(email, password, this.GetCancellationTokenOnDestroy()).Forget();

        public void AnonymousLoginClient() => AnonymousLoginClientAsync(this.GetCancellationTokenOnDestroy()).Forget();

        public void LogoutClient() => LogoutClientAsync(this.GetCancellationTokenOnDestroy()).Forget();
        
        [Button]
        [PropertyOrder(98)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<bool> CheckClientAuthAsync(CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Client.IsValid())
                return false;

            var accessToken = AuthStorage.Client.GetAccessToken();

            var response = await RestUtils.GetAsync<ApiResponse<AuthUserInfo>>(
                ClientAuthMeUrl,
                authToken: accessToken,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccess && response.Result?.Status == 0 && response.Result?.Data != null)
                return true;

            Debug.LogWarning("[ServiceHub] Auth check failed — token is no longer valid. Logging out.");
            await LogoutClientAsync(cancellationToken);
            return false;
        }

        public void TryLoadStoredClientToken()
        {
            if (!IsLoggedIn) return;
            var userId = AuthStorage.Client.GetInfo<AuthUserInfo>()?.Id
                         ?? JwtUtils.GetClaim(AuthStorage.Client.GetAccessToken(), "sub")
                         ?? "";
            if (!string.IsNullOrEmpty(userId))
                _local = new PlayerPrefsSaveDataStore($"uc.savedata.{userId}.");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<NetworkResponse<LoginResponse>> LoginClientAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default
        )
        {
            var request = new LoginRequest(_AppSecret, email, password);

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, LoginRequest>(
                ClientAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleClientLoginResponse(response);
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Auth")]
        public async UniTask<NetworkResponse<LoginResponse>> AnonymousLoginClientAsync(
            CancellationToken cancellationToken = default
        )
        {
            var token = GetOrCreateAnonymousToken();
            var request = new AnonymousLoginRequest(_AppSecret, token);

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, AnonymousLoginRequest>(
                ClientAnonymousAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleClientLoginResponse(response);
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
        
        public async UniTask LogoutClientAsync(CancellationToken cancellationToken = default)
        {
            var effectiveToken = cancellationToken == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : cancellationToken;
            
            await FlushAllDirtyEntries(effectiveToken);

            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            if (networkManager != null && networkManager.ConnectionState == ConnectionState.Connected)
            {
                networkManager.StopRuntime();
                Debug.Log("[ServiceHub] Disconnected from server as part of logout.");
            }
            
            AuthStorage.Client.Clear();
            _local = new PlayerPrefsSaveDataStore();
        }

        private NetworkResponse<LoginResponse> HandleClientLoginResponse(NetworkResponse<ApiResponse<LoginResponse>> response)
        {
            // Network-level failure (timeout, DNS, etc.)
            if (!response.IsSuccess && response.Result is null)
            {
                Debug.LogError($"[ServiceHub] Login request failed: {response.ErrorMessage}");
                OnClientLoginFailed?.Invoke(response.StatusCode, response.ErrorMessage);
                return NetworkResponse<LoginResponse>.Error(response.StatusCode, response.ErrorMessage);
            }

            var apiResponse = response.Result;

            // API-level failure (invalid credentials, missing argument, etc.)
            if (apiResponse == null || apiResponse.Status != 0 || apiResponse.Data == null)
            {
                var message = apiResponse?.Message ?? "Unknown error";
                var statusCode = apiResponse?.Status ?? -1;
                Debug.LogError($"[ServiceHub] Login failed ({statusCode}): {message}");
                OnClientLoginFailed?.Invoke(statusCode, message);
                return NetworkResponse<LoginResponse>.Error(response.StatusCode, message);
            }

            var loginResponse = apiResponse.Data;
            var accessToken = loginResponse.AccessToken;
            var userInfo = loginResponse.AuthUser;

            // Extract expiration from the JWT itself, fall back to expires_in
            var expUnix = JwtUtils.GetUnixExp(accessToken);
            var infoJson = userInfo != null ? JsonConvert.SerializeObject(userInfo) : null;
            AuthStorage.Client.Save(accessToken, null, expUnix, loginResponse.ExpiresIn, infoJson);
            var userId = userInfo?.Id ?? JwtUtils.GetClaim(accessToken, "sub") ?? "";
            _local = new PlayerPrefsSaveDataStore($"uc.savedata.{userId}.");

            var email = userInfo?.Email ?? JwtUtils.GetClaim(accessToken, "email") ?? "";
            if (!string.IsNullOrEmpty(email))
                AuthStorage.Client.SetLastEmail(email);

            Debug.Log("[ServiceHub] Login successful.");
            OnClientLoginSucceeded?.Invoke(loginResponse);

            var result = NetworkResponse<LoginResponse>.Success(response.StatusCode, loginResponse);
            Debug.Log($"[ServiceHub] Login response (formatted): {JsonConvert.SerializeObject(result, Formatting.Indented)}");
            return result;
        }
    }
}
