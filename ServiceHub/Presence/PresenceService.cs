// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.ServiceHub.Auth;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR;

namespace EDIVE.ServiceHub.Presence
{
    public delegate UniTask<PresenceReply> PresenceCommandHandler(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken);

    // Tells ServiceHub this app is running and receives its commands, see ServiceHub docs/streaming-contract.md
    public class PresenceService : MonoBehaviour, IServiceHubModule
    {
        public const string SESSION_SECRET_HEADER = "X-Session-Secret";

        // ServiceHub waits up to 20 s for the other side
        private const int ANSWER_TIMEOUT_SECONDS = 30;

        private readonly Dictionary<string, PresenceCommandHandler> _handlers = new();
        private string _instanceId;
        private string _joinCode;
        private string _guestPass;

        public ServiceHubSettings Settings { get; private set; }

        [ShowInInspector, ReadOnly]
        public bool IsEnabled => Settings != null && !string.IsNullOrEmpty(Settings.AppSecret) && !IsUnsupported && AppServices.IsEnabled(AppService.Streams);

        // ServiceHub has no presence, stays off until restart
        [ShowInInspector, ReadOnly]
        public bool IsUnsupported { get; private set; }

        public PresenceClient Player { get; private set; }
        public PresenceClient Host { get; private set; }

        [ShowInInspector, ReadOnly]
        public string InstanceId => _instanceId ??= CreateInstanceId();

        [ShowInInspector, ReadOnly]
        public string JoinCode => _joinCode;

        public string GuestPass => _guestPass;
        public string Device => DetectDevice();

        public event Action<PresenceClient> HostStarted;
        public event Action<PresenceClient> HostStopped;

        public void Initialize(ServiceHubSettings settings)
        {
            Settings = settings;
            if (string.IsNullOrEmpty(Settings?.AppSecret))
                return;

            Player = new PresenceClient(this, null);
            AppServices.Changed -= RefreshRunning;
            AppServices.Changed += RefreshRunning;
            RefreshRunning();
        }

        // Streams can be turned on or off for the app at any time
        private void RefreshRunning()
        {
            if (Player == null)
                return;
            if (IsEnabled)
            {
                if (!Player.IsRunning)
                    Player.Start(destroyCancellationToken);
                return;
            }
            Player.Stop();
            StopHost();
        }

        public void SetJoinCode(string joinCode)
        {
            if (string.IsNullOrEmpty(joinCode))
                joinCode = null;
            if (_joinCode == joinCode)
                return;
            _joinCode = joinCode;
            Player?.MarkDirty();
        }

        // Kept in memory only, a signed-in user's token goes first
        public void SetGuestPass(string pass)
        {
            if (string.IsNullOrEmpty(pass))
                pass = null;
            if (_guestPass == pass)
                return;
            _guestPass = pass;
            Player?.MarkDirty();
        }

        public void StartHost(string sessionSecret)
        {
            if (!IsEnabled || string.IsNullOrEmpty(sessionSecret))
                return;

            StopHost();
            Host = new PresenceClient(this, sessionSecret);
            Host.Start(destroyCancellationToken);
            HostStarted?.Invoke(Host);
        }

        public void StopHost()
        {
            if (Host == null)
                return;

            var host = Host;
            Host = null;
            host.Stop();
            HostStopped?.Invoke(host);
        }

        public void RegisterHandler(string type, PresenceCommandHandler handler)
        {
            _handlers[type] = handler;
        }

        public void UnregisterHandler(string type, PresenceCommandHandler handler)
        {
            if (_handlers.TryGetValue(type, out var current) && current == handler)
                _handlers.Remove(type);
        }

        public bool TryGetHandler(string type, out PresenceCommandHandler handler) => _handlers.TryGetValue(type, out handler);

        // LiveKit room of the current session, or the personal room outside one
        public async UniTask<NetworkResponse<StreamTokenResponse>> RequestStreamTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || Player == null)
                return NetworkResponse<StreamTokenResponse>.Error(0, "Presence is off");

            var request = new StreamTokenRequest { JoinCode = JoinCode, InstanceId = InstanceId };
            var url = $"{Settings.ServiceBaseUrl}/stream/token";
            return PresenceClient.Unwrap(await Player.SendAsync((token, headers) => RestUtils.PostAsync<ApiResponse<StreamTokenResponse>, StreamTokenRequest>(
                url, request, token, headers, Settings.ApiTimeoutSeconds, cancellationToken)));
        }

        // What is live in the session, without joining LiveKit
        public async UniTask<NetworkResponse<LiveSourcesResponse>> GetLiveSourcesAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || Player == null)
                return NetworkResponse<LiveSourcesResponse>.Error(0, "Presence is off");

            var url = $"{Settings.ServiceBaseUrl}/stream/live?instance_id={Uri.EscapeDataString(InstanceId)}";
            return PresenceClient.Unwrap(await Player.SendAsync((token, headers) => RestUtils.GetAsync<ApiResponse<LiveSourcesResponse>>(
                url, token, headers, Settings.ApiTimeoutSeconds, cancellationToken)));
        }

        // Asks someone in the session to share their view, waits for their answer
        public async UniTask<NetworkResponse<StreamActionResponse>> RequestViewAsync(string identity, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || Player == null)
                return NetworkResponse<StreamActionResponse>.Error(0, "Presence is off");

            var request = new RequestViewRequest { Identity = identity, InstanceId = InstanceId };
            var url = $"{Settings.ServiceBaseUrl}/stream/request-view";
            return PresenceClient.Unwrap(await Player.SendAsync((token, headers) => RestUtils.PostAsync<ApiResponse<StreamActionResponse>, RequestViewRequest>(
                url, request, token, headers, ANSWER_TIMEOUT_SECONDS, cancellationToken)));
        }

        // Shows a source on a display in the session, asks its presenter to hand over first
        public async UniTask<NetworkResponse<StreamActionResponse>> SetDisplayAsync(string displayId, string source, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || Player == null)
                return NetworkResponse<StreamActionResponse>.Error(0, "Presence is off");

            var request = new DisplayRequest { DisplayId = displayId, Source = source, InstanceId = InstanceId };
            var url = $"{Settings.ServiceBaseUrl}/stream/display";
            return PresenceClient.Unwrap(await Player.SendAsync((token, headers) => RestUtils.PostAsync<ApiResponse<StreamActionResponse>, DisplayRequest>(
                url, request, token, headers, ANSWER_TIMEOUT_SECONDS, cancellationToken)));
        }

        internal void MarkUnsupported()
        {
            if (IsUnsupported)
                return;

            IsUnsupported = true;
            Debug.Log("[Presence] Presence is not available, turning it off.");
            Player?.Stop();
            StopHost();
        }

        public static string DetectDevice()
        {
            if (XRSettings.isDeviceActive)
                return PresenceDevice.HEADSET;
            return Application.isMobilePlatform ? PresenceDevice.MOBILE : PresenceDevice.DESKTOP;
        }

        private static string CreateInstanceId()
        {
            var installId = ClientAuthService.GetOrCreateDeviceId();
#if UNITY_EDITOR
            // ParrelSync clones share PlayerPrefs
            installId += Application.dataPath;
#endif
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(installId));
            var builder = new StringBuilder(32);
            for (var i = 0; i < 16; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        private void OnDestroy()
        {
            AppServices.Changed -= RefreshRunning;
            Player?.Stop();
            StopHost();
        }
    }
}
