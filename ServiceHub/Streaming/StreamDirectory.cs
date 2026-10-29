// Author: František Holubec
// Created: 21.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.Players;
using EDIVE.ServiceHub.Presence;
using EDIVE.ServiceHub.Streaming.Players;

namespace EDIVE.ServiceHub.Streaming
{
    // People and streams in the session for the pickers: who is live, who can be asked
    public class StreamDirectory : IDisposable
    {
        public const string PRESENT_PREFIX = "present:";
        private const string PRESENT_KIND = "present";
        private const float POLL_INTERVAL = 3f;

        private readonly HashSet<NetworkStreamPlayer> _players = new();
        private readonly HashSet<string> _asked = new();
        private readonly Dictionary<string, (StreamRequestError Error, int Id)> _askFailures = new();
        private readonly CancellationTokenSource _disposed = new();
        private int _askFailureId;
        private readonly HashSet<string> _allowedSources = new();
        private readonly Dictionary<string, Action<string>> _whenLive = new();
        private readonly IDisposable _streamingRegistration;
        private readonly IDisposable _playersRegistration;
        private readonly IDisposable _serviceHubRegistration;
        private NetworkPlayerManager _playerManager;
        private List<LiveSource> _serverSources = new();
        private CancellationTokenSource _poll;

        public StreamingManager Streaming { get; private set; }
        public PresenceService Presence { get; private set; }
        public bool IsAvailable => Streaming != null && Streaming.IsAvailable;
        public bool CanAskAnyone => Presence != null && Presence.IsEnabled;

        public event Action Changed;

        private const float ASK_FAILURE_SECONDS = 5f;

        public StreamDirectory()
        {
            _streamingRegistration = AppCore.Services.WhenRegistered<StreamingManager>(BindStreaming);
            _playersRegistration = AppCore.Services.WhenRegistered<NetworkPlayerManager>(BindPlayers);
            _serviceHubRegistration = AppCore.Services.WhenRegistered<ServiceHubManager>(OnServiceHubRegistered);
        }

        public void Dispose()
        {
            _disposed.Cancel();
            _disposed.Dispose();
            _streamingRegistration?.Dispose();
            _playersRegistration?.Dispose();
            _serviceHubRegistration?.Dispose();
            SetPolling(false);
            BindStreaming(null);
            BindPlayers(null);
            Changed = null;
        }

        // Forgets asks and allowed shares, e.g. when a pooled window closes
        public void Reset()
        {
            _asked.Clear();
            _allowedSources.Clear();
            _whenLive.Clear();
        }

        // Asks ServiceHub what is live while a picker is open
        public void SetPolling(bool active)
        {
            if (active == (_poll != null))
                return;
            if (active)
            {
                _poll = new CancellationTokenSource();
                PollAsync(_poll.Token).Forget();
                return;
            }
            _poll.Cancel();
            _poll.Dispose();
            _poll = null;
            _serverSources = new List<LiveSource>();
        }

        public bool IsAsked(string identity) => identity != null && _asked.Contains(identity);

        // Declined or failed asks stay on the row for a few seconds
        public bool TryGetAskFailure(string identity, out StreamRequestError error)
        {
            error = StreamRequestError.None;
            if (identity == null || !_askFailures.TryGetValue(identity, out var failure))
                return false;
            error = failure.Error;
            return true;
        }

        // Shares of people in the session, then web presentations on displays
        public List<StreamLiveItem> GetLive(string sharingInfo, string presentingInfo)
        {
            var live = _players
                .Where(p => p != null && !p.isOwner && p.State.IsSharing)
                .OrderBy(GetName)
                .Select(p => new StreamLiveItem(GetName(p), p.State.Source, p.State.Identity, sharingInfo, p.State.Open || _allowedSources.Contains(p.State.Source)))
                .ToList();

            if (Streaming == null)
                return live;
            var presentations = Streaming.Screens
                .Select(s => s.State)
                .Where(s => s != null && !s.IsEmpty && s.Source.StartsWith(PRESENT_PREFIX, StringComparison.Ordinal))
                .GroupBy(s => s.Source)
                .Select(g => g.First());
            foreach (var state in presentations)
                live.Add(new StreamLiveItem(state.PresenterName, state.Source, null, presentingInfo, true));

            // Presented to the whole session, not on a display yet. The room is instant while joined, ServiceHub otherwise
            foreach (var source in Streaming.Room.GetWebSources(PRESENT_PREFIX).OrderBy(s => s.Name))
            {
                if (live.All(l => l.Source != source.Source))
                    live.Add(new StreamLiveItem(source.Name, source.Source, null, presentingInfo, true));
            }
            foreach (var source in _serverSources.Where(s => s.Kind == PRESENT_KIND).OrderBy(s => s.Name))
            {
                if (live.All(l => l.Source != source.Source))
                    live.Add(new StreamLiveItem(source.Name, source.Source, null, presentingInfo, source.Open));
            }
            return live;
        }

        public List<NetworkStreamPlayer> GetOthers()
        {
            return _players
                .Where(p => p != null && !p.isOwner && !p.State.IsSharing)
                .OrderBy(GetName)
                .ToList();
        }

        // Asks to watch, onLive gets the source once their share reaches us
        public async UniTask<bool> AskAsync(string identity, Action<string> onLive, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identity) || !CanAskAnyone || !_asked.Add(identity))
                return false;
            Changed?.Invoke();

            _askFailures.Remove(identity);
            var response = await Presence.RequestViewAsync(identity, cancellationToken);
            _asked.Remove(identity);
            var allowed = response.IsSuccess && response.Result?.Result == PresenceResult.ALLOW;
            if (!allowed && !cancellationToken.IsCancellationRequested)
            {
                var error = !response.IsSuccess ? StreamRequestErrors.FromStatus(response.StatusCode, response.ApiStatus)
                    : response.Result?.Result == PresenceResult.DENY ? StreamRequestError.Declined
                    : response.Result?.Result == PresenceResult.TIMEOUT ? StreamRequestError.NoAnswer
                    : StreamRequestError.Failed;
                _askFailures[identity] = (error, ++_askFailureId);
                ExpireAskFailureAsync(identity, _askFailureId).Forget();
            }
            if (allowed && onLive != null)
            {
                _whenLive[identity] = onLive;
                var player = _players.FirstOrDefault(p => p.State.Identity == identity);
                if (player != null)
                    OnPlayerChanged(player);
            }
            Changed?.Invoke();
            return allowed;
        }

        private async UniTaskVoid ExpireAskFailureAsync(string identity, int id)
        {
            if (await UniTask.Delay(TimeSpan.FromSeconds(ASK_FAILURE_SECONDS), true, cancellationToken: _disposed.Token).SuppressCancellationThrow())
                return;
            // A newer failure keeps its own time
            if (!_askFailures.TryGetValue(identity, out var current) || current.Id != id)
                return;
            _askFailures.Remove(identity);
            Changed?.Invoke();
        }

        private async UniTaskVoid PollAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (CanAskAnyone)
                    {
                        var response = await Presence.GetLiveSourcesAsync(cancellationToken);
                        if (response.IsSuccess)
                        {
                            _serverSources = response.Result?.Sources ?? new List<LiveSource>();
                            Changed?.Invoke();
                        }
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(POLL_INTERVAL), true, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        public static string GetName(NetworkStreamPlayer player) => player.Player.AuthUserInfo?.Name ?? player.name;

        private void OnServiceHubRegistered(ServiceHubManager serviceHub)
        {
            Presence = serviceHub.Presence;
            Changed?.Invoke();
        }

        private void BindStreaming(StreamingManager streaming)
        {
            if (Streaming != null)
            {
                Streaming.ViewShare.Changed -= OnStreamingChanged;
                Streaming.ScreensChanged -= OnStreamingChanged;
                Streaming.Room.ConnectionChanged -= OnStreamingChanged;
                Streaming.Room.PublicationsChanged -= OnStreamingChanged;
            }
            Streaming = streaming;
            if (Streaming != null)
            {
                Streaming.ViewShare.Changed += OnStreamingChanged;
                Streaming.ScreensChanged += OnStreamingChanged;
                Streaming.Room.ConnectionChanged += OnStreamingChanged;
                Streaming.Room.PublicationsChanged += OnStreamingChanged;
            }
            Changed?.Invoke();
        }

        private void BindPlayers(NetworkPlayerManager playerManager)
        {
            if (_playerManager != null)
            {
                _playerManager.PlayerRegistered -= OnPlayerRegistered;
                _playerManager.PlayerUnregistered -= OnPlayerUnregistered;
            }
            foreach (var player in _players)
            {
                // Destroyed with the scene
                if (player == null)
                    continue;
                player.Changed -= OnPlayerChanged;
                if (player.Player != null)
                    player.Player.AuthUserInfoChanged -= OnUserInfoChanged;
            }
            _players.Clear();

            _playerManager = playerManager;
            if (_playerManager != null)
            {
                _playerManager.PlayerRegistered += OnPlayerRegistered;
                _playerManager.PlayerUnregistered += OnPlayerUnregistered;
                foreach (var player in _playerManager.CurrentPlayers)
                    OnPlayerRegistered(player);
            }
            Changed?.Invoke();
        }

        private void OnPlayerRegistered(NetworkPlayerController player)
        {
            if (player == null || !player.TryGetComponent<NetworkStreamPlayer>(out var streamPlayer) || !_players.Add(streamPlayer))
                return;
            streamPlayer.Changed += OnPlayerChanged;
            player.AuthUserInfoChanged += OnUserInfoChanged;
            Changed?.Invoke();
        }

        private void OnPlayerUnregistered(NetworkPlayerController player)
        {
            if (player == null || !player.TryGetComponent<NetworkStreamPlayer>(out var streamPlayer) || !_players.Remove(streamPlayer))
                return;
            streamPlayer.Changed -= OnPlayerChanged;
            player.AuthUserInfoChanged -= OnUserInfoChanged;
            Changed?.Invoke();
        }

        private void OnPlayerChanged(NetworkStreamPlayer player)
        {
            var state = player.State;
            if (state.IsSharing && state.Identity != null && _whenLive.Remove(state.Identity, out var onLive))
            {
                _allowedSources.Add(state.Source);
                onLive(state.Source);
            }
            Changed?.Invoke();
        }

        private void OnUserInfoChanged(object _) => Changed?.Invoke();

        private void OnStreamingChanged(object _) => Changed?.Invoke();
    }

    public readonly struct StreamLiveItem
    {
        public string Name { get; }
        public string Source { get; }
        public string Identity { get; }
        public string Info { get; }

        // Open shares and presentations, a share for someone else needs Ask
        public bool CanWatch { get; }

        public StreamLiveItem(string name, string source, string identity, string info, bool canWatch)
        {
            Name = name;
            Source = source;
            Identity = identity;
            Info = info;
            CanWatch = canWatch;
        }
    }
}
