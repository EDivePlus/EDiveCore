// Author: Michal Petr
// Created: 29.04.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
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
        public static bool IsServerLoggedIn => AuthStorage.Server.IsValid();

        public event Action<ServerLoginResponse> OnServerLoginSucceeded;
        public event Action<long, string> OnServerLoginFailed;

        private string ServerAuthLoginUrl => $"{ServiceBaseUrl}/server/login";
        private string ServerAuthStatusUrl => $"{ServiceBaseUrl}/server/status";

        public void LoginServer(string serverId, string secret) =>
            LoginServerAsync(serverId, secret, this.GetCancellationTokenOnDestroy()).Forget();

        public void LogoutServer() =>
            LogoutServerAsync(this.GetCancellationTokenOnDestroy()).Forget();

        [Button]
        [PropertyOrder(98)]
        [EnhancedBoxGroup("ServerAuth", Color = "@ColorTools.Magenta", SpaceBefore = 8)]
        public async UniTask<bool> CheckServerAuthAsync(CancellationToken cancellationToken = default)
        {
            if (!AuthStorage.Server.IsValid())
                return false;

            var accessToken = AuthStorage.Server.GetAccessToken();

            var response = await RestUtils.PostAsync<ApiResponse<ServerCredentialResponse>, object>(
                ServerAuthStatusUrl,
                null,
                authToken: accessToken,
                headers: null,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccess && response.Result?.Status == 0 && response.Result?.Data != null)
                return true;

            Debug.LogWarning("[ServiceHub] Server auth check failed — token is no longer valid. Logging out.");
            await LogoutServerAsync(cancellationToken);
            return false;
        }

        public void TryLoadStoredServerToken()
        {
            if (!IsServerLoggedIn) return;
            var info = AuthStorage.Server.GetInfo<ServerLoginResponse>();
            var serverId = info?.ServerId
                           ?? JwtUtils.GetClaim(AuthStorage.Server.GetAccessToken(), "server_id")
                           ?? "";
            if (!string.IsNullOrEmpty(serverId))
                _serverLocal = new PlayerPrefsSaveDataStore($"uc.savedata.server.{serverId}.");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("ServerAuth")]
        public async UniTask<NetworkResponse<ServerLoginResponse>> LoginServerAsync(
            string serverId,
            string secret,
            CancellationToken cancellationToken = default
        )
        {
            var request = new ServerLoginRequest(serverId, secret);

            var response = await RestUtils.PostAsync<ApiResponse<ServerLoginResponse>, ServerLoginRequest>(
                ServerAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: _AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleServerLoginResponse(response);
        }

        public async UniTask LogoutServerAsync(CancellationToken cancellationToken = default)
        {
            var effectiveToken = cancellationToken == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : cancellationToken;

            await FlushAllServerDirtyEntries(effectiveToken);

            AuthStorage.Server.Clear();
            _serverLocal = new PlayerPrefsSaveDataStore("uc.savedata.server.");
        }

        private NetworkResponse<ServerLoginResponse> HandleServerLoginResponse(NetworkResponse<ApiResponse<ServerLoginResponse>> response)
        {
            // Network-level failure (timeout, DNS, etc.)
            if (!response.IsSuccess && response.Result is null)
            {
                Debug.LogError($"[ServiceHub] Server login request failed: {response.ErrorMessage}");
                OnServerLoginFailed?.Invoke(response.StatusCode, response.ErrorMessage);
                return NetworkResponse<ServerLoginResponse>.Error(response.StatusCode, response.ErrorMessage);
            }

            var apiResponse = response.Result;

            // API-level failure (invalid credentials, missing argument, etc.)
            if (apiResponse == null || apiResponse.Status != 0 || apiResponse.Data == null)
            {
                var message = apiResponse?.Message ?? "Unknown error";
                var statusCode = apiResponse?.Status ?? -1;
                Debug.LogError($"[ServiceHub] Server login failed ({statusCode}): {message}");
                OnServerLoginFailed?.Invoke(statusCode, message);
                return NetworkResponse<ServerLoginResponse>.Error(response.StatusCode, message);
            }

            var loginResponse = apiResponse.Data;
            var accessToken = loginResponse.AccessToken;

            var expUnix = JwtUtils.GetUnixExp(accessToken);
            var infoJson = JsonConvert.SerializeObject(loginResponse);
            AuthStorage.Server.Save(accessToken, null, expUnix, loginResponse.ExpiresIn, infoJson);

            var serverId = loginResponse.ServerId
                           ?? JwtUtils.GetClaim(accessToken, "server_id")
                           ?? "";
            _serverLocal = new PlayerPrefsSaveDataStore($"uc.savedata.server.{serverId}.");

            Debug.Log("[ServiceHub] Server login successful.");
            OnServerLoginSucceeded?.Invoke(loginResponse);

            return NetworkResponse<ServerLoginResponse>.Success(response.StatusCode, loginResponse);
        }
    }
}
