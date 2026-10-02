// Author: František Holubec
// Created: 02.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.ServiceHub.Connectivity
{
    public class ConnectivityService : MonoBehaviour, IServiceHubModule
    {
        [SerializeField]
        [PropertyTooltip("Path on service base url. Must return 2xx.")]
        private string _ServicePingPath = "/probe/ping";

        [SerializeField]
        [PropertyTooltip("HTTPS urls. Any HTTP answer means internet works.")]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<string> _InternetCheckUrls = new()
        {
            "https://connectivitycheck.gstatic.com/generate_204",
            "https://www.cloudflare.com/cdn-cgi/trace"
        };

        [PropertySpace]
        [SerializeField]
        [MinValue(1)]
        [SuffixLabel("s", true)]
        private int _CheckTimeoutSeconds = 4;

        [SerializeField]
        [MinValue(1)]
        [SuffixLabel("s", true)]
        private float _ConnectedInterval = 20f;

        [SerializeField]
        [MinValue(1)]
        [SuffixLabel("s", true)]
        private float _DisconnectedInterval = 4f;

        [PropertySpace]
        [ShowInInspector]
        [ReadOnly]
        public ConnectivityState State { get; private set; } = ConnectivityState.Unknown;

        [ShowInInspector]
        [ReadOnly]
        public bool IsChecking { get; private set; }

        public bool IsConnected => State == ConnectivityState.Connected;

        public event Action<ConnectivityState> StateChanged;
        public event Action<bool> IsCheckingChanged;

        protected ServiceHubSettings Settings { get; private set; }

        private readonly HashSet<long> _serviceRequests = new();
        private bool _checkRequested;
        private bool _initialized;

        public void Initialize(ServiceHubSettings settings)
        {
            Settings = settings;
            if (_initialized)
                return;

            _initialized = true;
            RestUtils.OnRequestStarted += OnRequestStarted;
            RestUtils.OnRequestCompleted += OnRequestCompleted;
            RestUtils.OnRequestCancelled += OnRequestCancelled;
            MonitorLoop(destroyCancellationToken).Forget();
        }

        [Button]
        [EnhancedBoxGroup("Status")]
        public void RequestCheck() => _checkRequested = true;

        private void OnDestroy()
        {
            RestUtils.OnRequestStarted -= OnRequestStarted;
            RestUtils.OnRequestCompleted -= OnRequestCompleted;
            RestUtils.OnRequestCancelled -= OnRequestCancelled;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
                RequestCheck();
        }

        private async UniTaskVoid MonitorLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    _checkRequested = false;
                    await CheckAsync(cancellationToken);

                    var interval = IsConnected ? _ConnectedInterval : _DisconnectedInterval;
                    var nextCheckTime = Time.realtimeSinceStartup + interval;
                    await UniTask.WaitUntil(() => _checkRequested || Time.realtimeSinceStartup >= nextCheckTime, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask CheckAsync(CancellationToken cancellationToken)
        {
            SetChecking(true);
            try
            {
                SetState(await ResolveStateAsync(cancellationToken));
            }
            finally
            {
                SetChecking(false);
            }
        }

        private async UniTask<ConnectivityState> ResolveStateAsync(CancellationToken cancellationToken)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return ConnectivityState.NoConnection;

            if (await IsServiceReachableAsync(cancellationToken))
                return ConnectivityState.Connected;

            return await IsInternetReachableAsync(cancellationToken)
                ? ConnectivityState.NoServiceConnection
                : ConnectivityState.NoConnection;
        }

        private async UniTask<bool> IsServiceReachableAsync(CancellationToken cancellationToken)
        {
            if (Settings == null || string.IsNullOrEmpty(Settings.ServiceBaseUrl))
                return false;

            var code = await GetResponseCodeAsync(Settings.ServiceBaseUrl + _ServicePingPath, UnityWebRequest.kHttpVerbHEAD, cancellationToken);
            return code is >= 200 and < 300;
        }

        private async UniTask<bool> IsInternetReachableAsync(CancellationToken cancellationToken)
        {
            foreach (var url in _InternetCheckUrls)
            {
                if (string.IsNullOrEmpty(url))
                    continue;
                if (await GetResponseCodeAsync(url, UnityWebRequest.kHttpVerbGET, cancellationToken) > 0)
                    return true;
            }
            return false;
        }

        private async UniTask<long> GetResponseCodeAsync(string url, string method, CancellationToken cancellationToken)
        {
            using var request = new UnityWebRequest(url, method);
            request.timeout = _CheckTimeoutSeconds;
            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            }
            catch (UnityWebRequestException) { }
            return request.responseCode;
        }

        // API errors like 401 or 404 still mean service is up, 5xx means it is down
        private static bool IsServiceAnswer(long code) => code is > 0 and < 500;

        private void OnRequestStarted(RequestStartedEvent e)
        {
            if (Settings != null && e.Url != null && e.Url.StartsWith(Settings.ServiceBaseUrl, StringComparison.OrdinalIgnoreCase))
                _serviceRequests.Add(e.RequestId);
        }

        private void OnRequestCompleted(RequestCompletedEvent e)
        {
            if (!_serviceRequests.Remove(e.RequestId))
                return;

            if (IsServiceAnswer(e.StatusCode))
                SetState(ConnectivityState.Connected);
            else if (IsConnected)
                RequestCheck();
        }

        private void OnRequestCancelled(RequestCancelledEvent e) => _serviceRequests.Remove(e.RequestId);

        private void SetState(ConnectivityState state)
        {
            if (State == state)
                return;
            State = state;
            StateChanged?.Invoke(state);
        }

        private void SetChecking(bool value)
        {
            if (IsChecking == value)
                return;
            IsChecking = value;
            IsCheckingChanged?.Invoke(value);
        }
    }
}
