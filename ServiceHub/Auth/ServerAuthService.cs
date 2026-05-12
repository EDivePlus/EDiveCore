// Author: Michal Petr
// Created: 12.05.2026

using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    public class ServerAuthService : AAuthService<ServerLoginResponse>
    {
        protected override AuthStorage Storage => AuthStorage.Server;
        protected override string LogScope => "Server login";

        private string ServerAuthLoginUrl => $"{Settings.ServiceBaseUrl}/server/login";
        private string ServerAuthStatusUrl => $"{Settings.ServiceBaseUrl}/server/status";

        public static string GetServerId()
        {
            var info = AuthStorage.Server.GetInfo<ServerLoginResponse>();
            return info?.ServerId
                   ?? JwtUtils.GetClaim(AuthStorage.Server.GetAccessToken(), "server_id")
                   ?? "";
        }

        public void LoginServer(string serverId, string secret) =>
            LoginServerAsync(serverId, secret, this.GetCancellationTokenOnDestroy()).Forget();

        public void LogoutServer() =>
            LogoutAsync(this.GetCancellationTokenOnDestroy()).Forget();

        public UniTask LogoutServerAsync(CancellationToken cancellationToken = default) =>
            LogoutAsync(cancellationToken);

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
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            if (response.IsSuccess && response.Result?.Status == 0 && response.Result?.Data != null)
                return true;

            Debug.LogWarning("[ServiceHub] Server auth check failed — token is no longer valid. Logging out.");
            await LogoutAsync(cancellationToken);
            return false;
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("ServerAuth")]
        public async UniTask<NetworkResponse<ServerLoginResponse>> LoginServerAsync(
            string serverId,
            string secret,
            CancellationToken cancellationToken = default)
        {
            var request = new ServerLoginRequest(serverId, secret);

            var response = await RestUtils.PostAsync<ApiResponse<ServerLoginResponse>, ServerLoginRequest>(
                ServerAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            return HandleLoginResponse(response);
        }

        public async UniTask PrepareServerAuthAsync(string serverId, string serverSecret, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverId) || string.IsNullOrEmpty(serverSecret))
            {
                Debug.LogError("[ServiceHub] ServerConfig is missing ServerID or ServerSecret.");
                return;
            }

            if (AuthStorage.Server.IsValid())
            {
                await CheckServerAuthAsync(cancellationToken);
            }

            if (!IsLoggedIn)
            {
                await LoginServerAsync(serverId, serverSecret, cancellationToken);
                if (!IsLoggedIn)
                    Debug.LogError($"[ServiceHub] Server '{serverId}' login from ServerConfig failed. Starting unauthenticated; server-scoped save data will not sync.");
            }
        }

        protected override void SaveLoginToStorage(ServerLoginResponse data)
        {
            var accessToken = data.AccessToken;
            var expUnix = JwtUtils.GetUnixExp(accessToken);
            var infoJson = JsonConvert.SerializeObject(data);
            AuthStorage.Server.Save(accessToken, null, expUnix, data.ExpiresIn, infoJson);
        }
    }
}
