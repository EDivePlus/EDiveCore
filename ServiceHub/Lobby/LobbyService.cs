// Author: Michal Petr
// Created: 12.05.2026

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Lobby
{
    public class LobbyService : MonoBehaviour, IServiceHubModule
    {
        protected ServiceHubSettings Settings { get; private set; }

        private string LobbyBaseUrl => $"{Settings.ServiceBaseUrl}/lobby";
        private string RegisterUrl => $"{LobbyBaseUrl}/register";
        private string HeartbeatUrl => $"{LobbyBaseUrl}/heartbeat";
        private string UpdateUrl => $"{LobbyBaseUrl}/update";
        private string DisposeUrl => $"{LobbyBaseUrl}/dispose";
        private string GetUrl => $"{LobbyBaseUrl}/get";
        private string QueryUrl => $"{LobbyBaseUrl}/query";

        private int RequestTimeoutSeconds => Settings.ApiTimeoutSeconds;

        public void Initialize(ServiceHubSettings settings)
        {
            Settings = settings;
        }

        private CancellationToken GetEffectiveToken(CancellationToken ct)
            => ct == CancellationToken.None ? this.GetCancellationTokenOnDestroy() : ct;

        public async UniTask<NetworkResponse<ServerRegistrationResponse>> RegisterServerAsync(
            RegisterServerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                return NetworkResponse<ServerRegistrationResponse>.Error(0, "Request is null");

            if (string.IsNullOrEmpty(request.AppSecret))
                request.AppSecret = Settings.AppSecret;

            var response = await RestUtils.PostAsync<ApiResponse<ServerRegistrationResponse>, RegisterServerRequest>(
                RegisterUrl,
                request,
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: GetEffectiveToken(cancellationToken)
            );
            return ApiResponseHelper.UnwrapApi(response, "RegisterServer");
        }

        public async UniTask<NetworkResponse<LobbyEmptyResponse>> HeartbeatAsync(
            string serverSecret,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverSecret))
                return NetworkResponse<LobbyEmptyResponse>.Error(0, "Server secret is empty");

            var response = await RestUtils.PostAsync<ApiResponse<LobbyEmptyResponse>, HeartbeatServerRequest>(
                HeartbeatUrl,
                new HeartbeatServerRequest(serverSecret),
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: GetEffectiveToken(cancellationToken)
            );
            return ApiResponseHelper.UnwrapApi(response, "HeartbeatServer");
        }

        public async UniTask<NetworkResponse<LobbyEmptyResponse>> UpdateServerAsync(
            UpdateServerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                return NetworkResponse<LobbyEmptyResponse>.Error(0, "Request is null");

            var response = await RestUtils.PostAsync<ApiResponse<LobbyEmptyResponse>, UpdateServerRequest>(
                UpdateUrl,
                request,
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: GetEffectiveToken(cancellationToken)
            );
            return ApiResponseHelper.UnwrapApi(response, "UpdateServer");
        }

        public async UniTask<NetworkResponse<LobbyEmptyResponse>> DisposeServerAsync(
            string serverSecret,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverSecret))
                return NetworkResponse<LobbyEmptyResponse>.Error(0, "Server secret is empty");

            var response = await RestUtils.PostAsync<ApiResponse<LobbyEmptyResponse>, DisposeServerRequest>(
                DisposeUrl,
                new DisposeServerRequest(serverSecret),
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: GetEffectiveToken(cancellationToken)
            );
            return ApiResponseHelper.UnwrapApi(response, "DisposeServer");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Lobby", Color = "@ColorTools.Aqua", SpaceBefore = 8)]
        public async UniTask<NetworkResponse<LobbyServerResponse>> GetServerAsync(
            string joinCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(joinCode))
                return NetworkResponse<LobbyServerResponse>.Error(0, "Join code is empty");

            var response = await RestUtils.PostAsync<ApiResponse<LobbyServerResponse>, GetServerRequest>(
                GetUrl,
                new GetServerRequest(joinCode, Settings.AppSecret),
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: GetEffectiveToken(cancellationToken)
            );
            return ApiResponseHelper.UnwrapApi(response, $"GetServer({joinCode})");
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Lobby")]
        public async UniTask<NetworkResponse<List<LobbyServerResponse>>> QueryServersAsync(
            int? count = null,
            int? skip = null,
            CancellationToken cancellationToken = default)
        {
            var response = await RestUtils.PostAsync<ApiResponse<List<LobbyServerResponse>>, QueryServersRequest>(
                QueryUrl,
                new QueryServersRequest(count, skip, Settings.AppSecret),
                authToken: null,
                headers: null,
                timeout: RequestTimeoutSeconds,
                cancellationToken: GetEffectiveToken(cancellationToken)
            );
            return ApiResponseHelper.UnwrapApi(response, "QueryServers");
        }
    }
}
