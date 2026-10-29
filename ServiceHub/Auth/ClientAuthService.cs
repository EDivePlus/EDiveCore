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
        private const string K_ANONYMOUS_USER_ID = "auth.anonymousUserId";
        private const string K_DEVICE_ID = "auth.deviceId";

        protected override AuthStorage Storage => AuthStorage.Client;
        protected override string LogScope => "Login";

        private string ClientAuthLoginUrl => $"{Settings.ServiceBaseUrl}/auth/login";
        private string ClientAnonymousAuthLoginUrl => $"{Settings.ServiceBaseUrl}/auth/anonymous-login";
        private string ClientAuthMeUrl => $"{Settings.ServiceBaseUrl}/auth/me";
        private string ClientRefreshUrl => $"{Settings.ServiceBaseUrl}/auth/refresh";
        private string ClientLogoutUrl => $"{Settings.ServiceBaseUrl}/auth/logout";
        private string ClientLinkCodeUrl => $"{Settings.ServiceBaseUrl}/auth/link-code";
        private string ClientDeviceCodeUrl => $"{Settings.ServiceBaseUrl}/auth/device-code";
        private string ClientDeviceCodePollUrl => $"{Settings.ServiceBaseUrl}/auth/device-code/poll";

        private UniTaskCompletionSource<bool> _refresh;

        // Session ended on the server: signed out elsewhere, expired or banned
        public event Action<ServiceHubStatus> SessionEnded;
        public event Action<AuthUserInfo> UserInfoChanged;

        // Why the server last ended the session, cleared on login
        public ServiceHubStatus? SessionEndReason { get; private set; }

        // Shown in the user's signed-in devices: computer name on desktop, model on headsets and phones
        private static string DeviceName => SystemInfo.deviceType == DeviceType.Desktop ? SystemInfo.deviceName : SystemInfo.deviceModel;

        public override void Initialize(ServiceHubSettings settings)
        {
            base.Initialize(settings);
            Storage.Refresher = RefreshAsync;
        }

        public static string GetUserId()
        {
            return AuthStorage.Client.GetInfo<AuthUserInfo>()?.Id
                   ?? JwtUtils.GetClaim(AuthStorage.Client.GetAccessToken(), "sub")
                   ?? "";
        }

        public bool TryGetAuthUserInfo(out AuthUserInfo userInfo)
        {
            if (!AuthStorage.Client.HasSession)
            {
                userInfo = null;
                return false;
            }

            userInfo = AuthStorage.Client.GetInfo<AuthUserInfo>();
            return userInfo != null;
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
            if (!Storage.HasSession)
                return false;

            var response = await Storage.SendAsync(token => RestUtils.GetAsync<ApiResponse<AuthUserInfo>>(
                ClientAuthMeUrl,
                authToken: token,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            ));

            if (response.IsSuccess && response.Result?.Status == 0 && response.Result?.Data != null)
            {
                SaveUserInfo(response.Result.Data);
                return true;
            }

            // Refresh found the session ended
            if (!Storage.HasSession)
                return false;
            // Offline or server error, token not proven bad
            if (!response.IsSuccess && response.StatusCode != 401 && response.StatusCode != 403)
            {
                Debug.LogWarning($"[ServiceHub] Auth check unreachable ({response.StatusCode}), keeping token.");
                return true;
            }

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
            var request = new LoginRequest(Settings.AppSecret, email, password, DeviceName, true, GetOrCreateDeviceId());

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
            var request = new AnonymousLoginRequest(Settings.AppSecret, token, DeviceName, true, GetOrCreateDeviceId());

            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, AnonymousLoginRequest>(
                ClientAnonymousAuthLoginUrl,
                request,
                authToken: null,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            // Key of an account linked meanwhile, the next anonymous login makes a new one
            if (response.Result?.Status == (int) ServiceHubStatus.AccountAlreadyLinked)
                ForgetAnonymousToken();
            return HandleLoginResponse(response);
        }

        // Code the user approves on the website, poll it with the device secret
        public async UniTask<NetworkResponse<DeviceCodeResponse>> RequestDeviceCodeAsync(CancellationToken cancellationToken = default)
        {
            var request = new DeviceCodeRequest(Settings.AppSecret, DeviceName, true, GetOrCreateDeviceId());
            var response = await RestUtils.PostAsync<ApiResponse<DeviceCodeResponse>, DeviceCodeRequest>(
                ClientDeviceCodeUrl,
                request,
                authToken: null,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            return ApiResponseHelper.UnwrapApi(response, "DeviceCode");
        }

        // Logs in once approved. Pending, denied and expired come back as ApiStatus without login events
        public async UniTask<NetworkResponse<LoginResponse>> PollDeviceCodeAsync(string deviceSecret, CancellationToken cancellationToken = default)
        {
            var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, DeviceCodePollRequest>(
                ClientDeviceCodePollUrl,
                new DeviceCodePollRequest(deviceSecret),
                authToken: null,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );

            // Offline or still waiting, no login events
            if (response.Result == null)
                return NetworkResponse<LoginResponse>.Error(response.StatusCode, response.ErrorMessage);
            if (response.Result.Status is (int) ServiceHubStatus.DeviceCodePending or (int) ServiceHubStatus.DeviceCodeDenied or (int) ServiceHubStatus.DeviceCodeExpired)
                return NetworkResponse<LoginResponse>.Error(response.StatusCode, response.Result.Message, apiStatus: response.Result.Status);

            return HandleLoginResponse(response);
        }

        // One refresh at a time, callers share the running one
        private async UniTask<bool> RefreshAsync()
        {
            if (_refresh != null)
                return await _refresh.Task;

            var source = _refresh = new UniTaskCompletionSource<bool>();
            var refreshed = false;
            ServiceHubStatus? ended = null;
            try
            {
                var response = await RestUtils.PostAsync<ApiResponse<LoginResponse>, RefreshRequest>(
                    ClientRefreshUrl,
                    new RefreshRequest(Storage.GetRefreshToken()),
                    authToken: null,
                    headers: null,
                    timeout: Settings.AuthTimeoutSeconds,
                    cancellationToken: destroyCancellationToken
                );

                if (response.IsSuccess && response.Result is {Status: 0, Data: not null} api)
                {
                    SaveLoginToStorage(api.Data);
                    refreshed = true;
                }
                else if (response.Result?.Status is (int) ServiceHubStatus.InvalidRefreshToken or (int) ServiceHubStatus.BannedFromApp)
                {
                    ended = (ServiceHubStatus) response.Result.Status;
                    Storage.Clear();
                }
                else
                {
                    Debug.LogWarning($"[ServiceHub] Refresh failed ({response.StatusCode}), keeping session: {response.ErrorMessage ?? response.Result?.Message}");
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _refresh = null;
                source.TrySetResult(refreshed);
            }

            if (ended.HasValue)
            {
                Debug.LogWarning($"[ServiceHub] Session ended ({ended.Value}), signing out.");
                SessionEndReason = ended;
                SessionEnded?.Invoke(ended.Value);
                LogoutAsync().Forget();
            }

            return refreshed;
        }

        protected override async UniTask EndRemoteSessionAsync(CancellationToken cancellationToken)
        {
            var token = await Storage.GetAccessTokenAsync();
            if (token == null)
                return;

            var response = await RestUtils.PostAsync<ApiResponse<object>, object>(
                ClientLogoutUrl,
                null,
                authToken: token,
                headers: null,
                timeout: Settings.AuthTimeoutSeconds,
                cancellationToken: cancellationToken
            );
            if (!response.IsSuccess)
                Debug.LogWarning($"[ServiceHub] Server logout failed ({response.StatusCode}), signing out locally.");
        }

        // New code for linking this anonymous account on the website, replaces the previous one
        public async UniTask<NetworkResponse<LinkCodeResponse>> RequestLinkCodeAsync(CancellationToken cancellationToken = default)
        {
            var response = await Storage.SendAsync(token => RestUtils.PostAsync<ApiResponse<LinkCodeResponse>, object>(
                ClientLinkCodeUrl,
                null,
                authToken: token,
                headers: null,
                timeout: Settings.ApiTimeoutSeconds,
                cancellationToken: cancellationToken
            ));
            return ApiResponseHelper.UnwrapApi(response, "RequestLinkCode");
        }

        public async UniTask<NetworkResponse<LinkCodeStatusResponse>> GetLinkCodeStatusAsync(CancellationToken cancellationToken = default)
        {
            var response = await Storage.SendAsync(token => RestUtils.GetAsync<ApiResponse<LinkCodeStatusResponse>>(
                ClientLinkCodeUrl,
                authToken: token,
                timeout: Settings.ApiTimeoutSeconds,
                cancellationToken: cancellationToken
            ));
            var result = ApiResponseHelper.UnwrapApi(response, "GetLinkCodeStatus");
            // Account is no longer anonymous
            if (result.IsSuccess && result.Result.State == LinkCodeState.Linked)
                await CheckClientAuthAsync(cancellationToken);
            return result;
        }

        protected override void SaveLoginToStorage(LoginResponse data)
        {
            var accessToken = data.AccessToken;
            var userInfo = data.AuthUser;
            var expUnix = JwtUtils.GetUnixExp(accessToken);
            var infoJson = userInfo != null ? JsonConvert.SerializeObject(userInfo) : null;

            AuthStorage.Client.Save(accessToken, data.RefreshToken, expUnix, data.ExpiresIn, infoJson);
            SessionEndReason = null;

            var email = userInfo?.Email ?? JwtUtils.GetClaim(accessToken, "email") ?? "";
            if (!string.IsNullOrEmpty(email))
                AuthStorage.Client.SetLastEmail(email);
            TrackAnonymousToken(userInfo);
            if (userInfo != null)
                UserInfoChanged?.Invoke(userInfo);
        }

        private void SaveUserInfo(AuthUserInfo userInfo)
        {
            Storage.SetInfoJson(JsonConvert.SerializeObject(userInfo));
            TrackAnonymousToken(userInfo);
            UserInfoChanged?.Invoke(userInfo);
        }

        // Once the anonymous account is linked its token is dead, the next anonymous login gets a fresh one
        private static void TrackAnonymousToken(AuthUserInfo userInfo)
        {
            if (userInfo == null || string.IsNullOrEmpty(userInfo.Id))
                return;

            if (userInfo.IsAnonymous)
            {
                PlayerPrefs.SetString(K_ANONYMOUS_USER_ID, userInfo.Id);
                PlayerPrefs.Save();
                return;
            }

            if (userInfo.Id != PlayerPrefs.GetString(K_ANONYMOUS_USER_ID, ""))
                return;
            ForgetAnonymousToken();
        }

        private static void ForgetAnonymousToken()
        {
            PlayerPrefs.DeleteKey(K_ANONYMOUS_TOKEN);
            PlayerPrefs.DeleteKey(K_ANONYMOUS_USER_ID);
            PlayerPrefs.Save();
        }

        public static string GetOrCreateAnonymousToken()
        {
            var existing = PlayerPrefs.GetString(K_ANONYMOUS_TOKEN, "");
            if (!string.IsNullOrEmpty(existing))
                return existing;

            var token = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(K_ANONYMOUS_TOKEN, token);
            PlayerPrefs.Save();
            return token;
        }

        // Per install, lets the server replace this device's previous session on login
        public static string GetOrCreateDeviceId()
        {
            var existing = PlayerPrefs.GetString(K_DEVICE_ID, "");
            if (!string.IsNullOrEmpty(existing))
                return existing;

            var id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(K_DEVICE_ID, id);
            PlayerPrefs.Save();
            return id;
        }
    }
}
