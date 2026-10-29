// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.ServiceHub.Auth;
using UnityEngine;

namespace EDIVE.ServiceHub.Presence
{
    // One presence in ServiceHub: a player with a user token or guest pass, or a session host with its secret
    public class PresenceClient
    {
        private const int PROTOCOL = 1;
        private const float REPORT_INTERVAL = 30f;
        private const float RETRY_DELAY = 5f;
        private const float UNAUTHORIZED_DELAY = 15f;
        private const int POLL_TIMEOUT_SECONDS = 40;
        private const int HANDLED_HISTORY = 128;

        private readonly PresenceService _service;
        private readonly string _sessionSecret;

        private List<PresenceDisplay> _displays = new();
        private List<PresenceSource> _sources = new();
        private List<string> _watching = new();

        private readonly HashSet<string> _handled = new();
        private readonly Queue<string> _handledOrder = new();

        private CancellationTokenSource _cancellation;
        private string _credentialKey;
        private long _after;
        private bool _dirty;
        private bool _reported;

        public bool IsHost => _sessionSecret != null;
        public bool IsReported => _reported;

        // From the last report
        public string Identity { get; private set; }
        public string Participant { get; private set; }
        public string JoinCode { get; private set; }

        public IReadOnlyList<PresenceDisplay> Displays => _displays;
        public IReadOnlyList<PresenceSource> Sources => _sources;
        public IReadOnlyList<string> Watching => _watching;

        public event Action<PresenceClient> Reported;

        private string BaseUrl => $"{_service.Settings.ServiceBaseUrl}/presence";
        private string InstanceQuery => IsHost ? "" : $"instance_id={Uri.EscapeDataString(_service.InstanceId)}";

        internal PresenceClient(PresenceService service, string sessionSecret)
        {
            _service = service;
            _sessionSecret = sessionSecret;
        }

        public void SetDisplays(IEnumerable<PresenceDisplay> displays)
        {
            _displays = displays?.ToList() ?? new List<PresenceDisplay>();
            MarkDirty();
        }

        public void SetSources(IEnumerable<PresenceSource> sources)
        {
            _sources = sources?.ToList() ?? new List<PresenceSource>();
            MarkDirty();
        }

        public void SetWatching(IEnumerable<string> sourceIds)
        {
            var watching = sourceIds?.ToList() ?? new List<string>();
            if (watching.SequenceEqual(_watching))
                return;
            _watching = watching;
            MarkDirty();
        }

        // Reports again right away
        public void MarkDirty() => _dirty = true;

        public async UniTask<NetworkResponse<PresenceEmptyResponse>> ReplyAsync(PresenceCommand command, PresenceReply reply, CancellationToken cancellationToken = default)
        {
            var query = IsHost ? "" : $"?{InstanceQuery}";
            var url = $"{BaseUrl}/commands/{Uri.EscapeDataString(command.Id)}{query}";
            return Unwrap(await SendAsync((token, headers) => RestUtils.PostAsync<ApiResponse<PresenceEmptyResponse>, PresenceReply>(
                url, reply, token, headers, _service.Settings.ApiTimeoutSeconds, cancellationToken)));
        }

        internal void Start(CancellationToken cancellationToken)
        {
            Stop();
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ReportLoop(_cancellation.Token).Forget();
            PollLoop(_cancellation.Token).Forget();
        }

        internal void Stop()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
            _reported = false;
        }

        private async UniTaskVoid ReportLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var credentialKey = GetCredentialKey();
                if (credentialKey == null)
                {
                    _reported = false;
                    await UniTask.Delay(TimeSpan.FromSeconds(1), true, cancellationToken: cancellationToken);
                    continue;
                }

                // New identity has its own command sequence
                if (credentialKey != _credentialKey)
                {
                    _credentialKey = credentialKey;
                    _after = 0;
                    _reported = false;
                }

                _dirty = false;
                var response = await ReportAsync(cancellationToken);
                var wait = REPORT_INTERVAL;
                if (response.IsSuccess)
                {
                    Identity = response.Result.Identity;
                    Participant = response.Result.Participant;
                    JoinCode = response.Result.JoinCode;
                    if (response.Result.ExpiresIn > 0)
                        wait = Mathf.Min(wait, response.Result.ExpiresIn / 2f);
                    _reported = true;
                    Reported?.Invoke(this);
                }
                else if (IsUnsupported(response))
                {
                    _service.MarkUnsupported();
                    return;
                }
                else
                {
                    _reported = false;
                    wait = response.StatusCode == 401 ? UNAUTHORIZED_DELAY : RETRY_DELAY;
                    Debug.LogWarning($"[Presence] Report failed ({response.StatusCode}, {response.ApiStatus}): {response.ErrorMessage}");
                }

                var next = UnityEngine.Time.realtimeSinceStartup + wait;
                await UniTask.WaitUntil(() => _dirty || UnityEngine.Time.realtimeSinceStartup >= next, cancellationToken: cancellationToken);
            }
        }

        private async UniTaskVoid PollLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.WaitUntil(() => _reported, cancellationToken: cancellationToken);

                var after = _after;
                var query = IsHost ? $"after={after}" : $"{InstanceQuery}&after={after}";
                var url = $"{BaseUrl}/commands?{query}";
                var response = Unwrap(await SendAsync((token, headers) => RestUtils.GetAsync<ApiResponse<PresenceCommandsResponse>>(
                    url, token, headers, POLL_TIMEOUT_SECONDS, cancellationToken)));

                if (response.IsSuccess)
                {
                    // Identity changed while waiting
                    if (after != _after)
                        continue;
                    foreach (var command in response.Result.Commands.OrderBy(c => c.Seq))
                    {
                        _after = Math.Max(_after, command.Seq);
                        if (MarkHandled(command.Id))
                            HandleCommand(command, cancellationToken).Forget();
                    }
                }
                else if (response.ApiStatus == (int) ServiceHubStatus.PresenceNotFound)
                {
                    _reported = false;
                    MarkDirty();
                }
                else if (IsUnsupported(response))
                {
                    _service.MarkUnsupported();
                    return;
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(RETRY_DELAY), true, cancellationToken: cancellationToken);
                }
            }
        }

        private async UniTaskVoid HandleCommand(PresenceCommand command, CancellationToken cancellationToken)
        {
            PresenceReply reply;
            try
            {
                reply = _service.TryGetHandler(command.Type, out var handler)
                    ? await handler(this, command, cancellationToken)
                    : command.Type == PresenceCommandType.NOTICE ? null : new PresenceReply(PresenceResult.UNSUPPORTED);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                reply = new PresenceReply(PresenceResult.FAILED);
            }

            if (reply == null || command.Remaining <= TimeSpan.Zero)
                return;

            var response = await ReplyAsync(command, reply, cancellationToken);
            if (!response.IsSuccess && response.ApiStatus != (int) ServiceHubStatus.CommandNotFound)
                Debug.LogWarning($"[Presence] Reply to {command.Type} failed ({response.StatusCode}): {response.ErrorMessage}");
        }

        private async UniTask<NetworkResponse<PresenceResponse>> ReportAsync(CancellationToken cancellationToken)
        {
            var request = new PresenceRequest
            {
                Protocol = PROTOCOL,
                InstanceId = IsHost ? null : _service.InstanceId,
                JoinCode = IsHost ? null : _service.JoinCode,
                Device = IsHost ? null : _service.Device,
                Displays = _displays,
                Sources = _sources,
                Watching = IsHost ? null : _watching
            };
            return Unwrap(await SendAsync((token, headers) => RestUtils.PostAsync<ApiResponse<PresenceResponse>, PresenceRequest>(
                BaseUrl, request, token, headers, _service.Settings.ApiTimeoutSeconds, cancellationToken)));
        }

        internal UniTask<NetworkResponse<T>> SendAsync<T>(Func<string, Dictionary<string, string>, UniTask<NetworkResponse<T>>> send)
        {
            if (IsHost)
                return send(null, new Dictionary<string, string> { [PresenceService.SESSION_SECRET_HEADER] = _sessionSecret });
            if (AuthStorage.Client.HasSession)
                return AuthStorage.Client.SendAsync(token => send(token, null));
            var guestPass = _service.GuestPass;
            return string.IsNullOrEmpty(guestPass)
                ? UniTask.FromResult(NetworkResponse<T>.Error(401, "No credential"))
                : send(guestPass, null);
        }

        private string GetCredentialKey()
        {
            if (IsHost)
                return "host";
            if (AuthStorage.Client.HasSession)
                return $"user:{ClientAuthService.GetUserId()}";
            var guestPass = _service.GuestPass;
            return string.IsNullOrEmpty(guestPass) ? null : $"guest:{guestPass}";
        }

        private bool MarkHandled(string id)
        {
            if (!_handled.Add(id))
                return false;
            _handledOrder.Enqueue(id);
            if (_handledOrder.Count > HANDLED_HISTORY)
                _handled.Remove(_handledOrder.Dequeue());
            return true;
        }

        // Older ServiceHub without presence, or Streams turned off for the app
        private static bool IsUnsupported<T>(NetworkResponse<T> response) =>
            response.StatusCode == 404 && response.ApiStatus == 0 || response.ApiStatus == (int) ServiceHubStatus.ServiceDisabled;

        internal static NetworkResponse<T> Unwrap<T>(NetworkResponse<ApiResponse<T>> response)
        {
            var api = response.Result;
            if (api == null)
                return NetworkResponse<T>.Error(response.StatusCode, response.ErrorMessage);
            if (api.Status != 0 || api.Data == null)
            {
                AppServices.HandleStatus(api.Status, AppService.Streams);
                return NetworkResponse<T>.Error(response.StatusCode, api.Message, apiStatus: api.Status);
            }
            return NetworkResponse<T>.Success(response.StatusCode, api.Data);
        }
    }
}
