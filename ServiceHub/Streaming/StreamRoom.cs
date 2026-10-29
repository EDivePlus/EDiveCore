// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Presence;
using LiveKit;
using UnityEngine;
using RoomOptions = LiveKit.RoomOptions;

namespace EDIVE.ServiceHub.Streaming
{
    // LiveKit room of the current session, joined only while something needs video
    public class StreamRoom
    {
        private const float RETRY_DELAY = 5f;
        private const float LEAVE_DELAY = 3f;
        // Watching again right after stopping keeps the subscription instead of racing it
        private const float UNSUBSCRIBE_DELAY = 2f;
        private const string KIND_ATTRIBUTE = "kind";
        private const string WEB_KIND = "web";

        private readonly PresenceService _presence;
        private readonly MonoBehaviour _runner;
        private readonly Dictionary<string, StreamFeed> _feeds = new();

        private Room _room;
        private CancellationTokenSource _loop;
        private string _joinCode;
        private int _holds;

        public bool IsConnected => _room != null && _room.IsConnected;
        public string Identity { get; private set; }
        public Room Room => IsConnected ? _room : null;

        public event Action<StreamRoom> ConnectionChanged;

        // A track was published or unpublished, or someone left
        public event Action<StreamRoom> PublicationsChanged;

        public StreamRoom(PresenceService presence, MonoBehaviour runner)
        {
            _presence = presence;
            _runner = runner;
        }

        public StreamFeed Acquire(string sourceId)
        {
            if (!_feeds.TryGetValue(sourceId, out var feed))
            {
                feed = new StreamFeed(sourceId, _runner);
                _feeds[sourceId] = feed;
                if (IsConnected)
                    SubscribeExisting(sourceId, true);
            }
            feed.RefCount++;
            EnsureLoop();
            ReportWatching();
            return feed;
        }

        public void Release(StreamFeed feed)
        {
            if (feed == null || !_feeds.TryGetValue(feed.SourceId, out var current) || current != feed)
                return;
            if (--feed.RefCount > 0)
                return;

            _feeds.Remove(feed.SourceId);
            feed.Dispose();
            ReportWatching();
            UnsubscribeLaterAsync(feed.SourceId).Forget();
        }

        // Stays in the room while publishing
        public void Hold()
        {
            _holds++;
            EnsureLoop();
        }

        public void Unhold()
        {
            _holds = Mathf.Max(0, _holds - 1);
        }

        private StreamRequestError _joinError;

        // None once connected, otherwise why the last join failed
        public async UniTask<StreamRequestError> WaitConnectedAsync(float timeoutSeconds, CancellationToken cancellationToken)
        {
            var end = UnityEngine.Time.realtimeSinceStartup + timeoutSeconds;
            await UniTask.WaitUntil(() => IsConnected || UnityEngine.Time.realtimeSinceStartup >= end, cancellationToken: cancellationToken);
            if (IsConnected)
                return StreamRequestError.None;
            return _joinError != StreamRequestError.None ? _joinError : StreamRequestError.Unreachable;
        }

        // Video published from browsers, only known while in the room
        public List<StreamRoomSource> GetWebSources(string prefix)
        {
            var sources = new List<StreamRoomSource>();
            if (!IsConnected)
                return sources;
            foreach (var participant in _room.RemoteParticipants.Values)
            {
                if (!participant.Attributes.TryGetValue(KIND_ATTRIBUTE, out var kind) || kind != WEB_KIND)
                    continue;
                foreach (var publication in participant.Tracks.Values)
                {
                    if (publication.Kind == LiveKit.Proto.TrackKind.KindVideo && publication.Name.StartsWith(prefix, StringComparison.Ordinal))
                        sources.Add(new StreamRoomSource(publication.Name, string.IsNullOrEmpty(participant.Name) ? participant.Identity : participant.Name));
                }
            }
            return sources;
        }

        public void Dispose()
        {
            _loop?.Cancel();
            _loop?.Dispose();
            _loop = null;
            foreach (var feed in _feeds.Values)
                feed.Dispose();
            _feeds.Clear();
            Leave();
        }

        private async UniTaskVoid UnsubscribeLaterAsync(string sourceId)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(UNSUBSCRIBE_DELAY), true);
            if (IsConnected && !_feeds.ContainsKey(sourceId))
                SubscribeExisting(sourceId, false);
        }

        private void ReportWatching() => _presence?.Player?.SetWatching(_feeds.Keys);

        private void EnsureLoop()
        {
            if (_loop != null)
                return;
            _loop = new CancellationTokenSource();
            RoomLoop(_loop.Token).Forget();
        }

        private async UniTaskVoid RoomLoop(CancellationToken cancellationToken)
        {
            var idleSince = -1f;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_feeds.Count == 0 && _holds == 0)
                    {
                        var now = UnityEngine.Time.realtimeSinceStartup;
                        if (idleSince < 0)
                            idleSince = now;
                        if (now - idleSince >= LEAVE_DELAY)
                            break;
                    }
                    else
                    {
                        idleSince = -1f;
                        // Session changed, the room belongs to the old one
                        if (IsConnected && _joinCode != _presence.JoinCode)
                            Leave();
                        if (!IsConnected && !await TryJoin(cancellationToken))
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(RETRY_DELAY), true, cancellationToken: cancellationToken);
                            continue;
                        }
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f), true, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Leave();
            _loop?.Dispose();
            _loop = null;
            if (_feeds.Count > 0 || _holds > 0)
                EnsureLoop();
        }

        private async UniTask<bool> TryJoin(CancellationToken cancellationToken)
        {
            if (_presence == null || !_presence.IsEnabled)
                return false;

            var joinCode = _presence.JoinCode;
            var response = await _presence.RequestStreamTokenAsync(cancellationToken);
            _joinError = response.IsSuccess ? StreamRequestError.None : StreamRequestErrors.FromStatus(response.StatusCode, response.ApiStatus);
            if (!response.IsSuccess)
            {
                if (response.ApiStatus != (int) ServiceHubStatus.NotSessionPlayer)
                    Debug.LogWarning($"[StreamRoom] Token failed ({response.StatusCode}, {response.ApiStatus}): {response.ErrorMessage}");
                return false;
            }

            var room = new Room();
            room.TrackPublished += OnTrackPublished;
            room.TrackUnpublished += OnTrackUnpublished;
            room.ParticipantDisconnected += OnParticipantDisconnected;
            room.TrackSubscribed += OnTrackSubscribed;
            room.TrackUnsubscribed += OnTrackUnsubscribed;
            room.TrackMuted += OnTrackMuted;
            room.TrackUnmuted += OnTrackUnmuted;
            room.ConnectionQualityChanged += OnConnectionQualityChanged;
            room.Reconnecting += OnReconnecting;
            room.Reconnected += OnReconnected;
            room.Disconnected += OnDisconnected;

            var connect = room.Connect(response.Result.Url, response.Result.Token, new RoomOptions { AutoSubscribe = false, AdaptiveStream = true });
            try
            {
                await connect.AsUniTask(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelling only stops the wait, the connect still finishes
                await connect.AsUniTask();
                Release(room);
                throw;
            }
            if (connect.IsError)
            {
                _joinError = StreamRequestError.Unreachable;
                Debug.LogWarning($"[StreamRoom] Can't join {response.Result.Room}.");
                Release(room);
                return false;
            }

            _room = room;
            _joinCode = joinCode;
            Identity = response.Result.Identity;
            foreach (var sourceId in _feeds.Keys)
                SubscribeExisting(sourceId, true);
            ConnectionChanged?.Invoke(this);
            return true;
        }

        private void Leave()
        {
            if (_room == null)
                return;
            var room = _room;
            _room = null;
            Identity = null;
            Release(room);
            foreach (var feed in _feeds.Values)
                feed.ClearTracks();
            ConnectionChanged?.Invoke(this);
        }

        private void Release(Room room)
        {
            room.TrackPublished -= OnTrackPublished;
            room.TrackUnpublished -= OnTrackUnpublished;
            room.ParticipantDisconnected -= OnParticipantDisconnected;
            room.TrackSubscribed -= OnTrackSubscribed;
            room.TrackUnsubscribed -= OnTrackUnsubscribed;
            room.TrackMuted -= OnTrackMuted;
            room.TrackUnmuted -= OnTrackUnmuted;
            room.ConnectionQualityChanged -= OnConnectionQualityChanged;
            room.Reconnecting -= OnReconnecting;
            room.Reconnected -= OnReconnected;
            room.Disconnected -= OnDisconnected;
            room.Disconnect();
            room.Dispose();
        }

        private void SubscribeExisting(string sourceId, bool subscribed)
        {
            foreach (var participant in _room.RemoteParticipants.Values)
            {
                foreach (var publication in participant.Tracks.Values)
                {
                    if (publication.Name != sourceId)
                        continue;
                    publication.SetSubscribed(subscribed);
                    // Still subscribed from an earlier feed, no new event comes
                    if (subscribed && publication.Track != null && _feeds.ContainsKey(sourceId))
                        OnTrackSubscribed(publication.Track, publication, participant);
                }
            }
        }

        private void OnTrackPublished(RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (_feeds.ContainsKey(publication.Name))
                publication.SetSubscribed(true);
            PublicationsChanged?.Invoke(this);
        }

        private void OnTrackUnpublished(RemoteTrackPublication publication, RemoteParticipant participant) => PublicationsChanged?.Invoke(this);

        private void OnParticipantDisconnected(Participant participant) => PublicationsChanged?.Invoke(this);

        private void OnTrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (!_feeds.TryGetValue(publication.Name, out var feed))
                return;
            if (track is RemoteVideoTrack)
                publication.SetVideoQuality(LiveKit.Proto.VideoQuality.High);
            feed.SetTrack(track);
        }

        private void OnTrackUnsubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (_feeds.TryGetValue(publication.Name, out var feed))
                feed.RemoveTrack(track);
        }

        private void OnTrackMuted(TrackPublication publication, Participant participant) => SetLoss(publication, StreamFeedLoss.Muted, true);

        private void OnTrackUnmuted(TrackPublication publication, Participant participant) => SetLoss(publication, StreamFeedLoss.Muted, false);

        private void OnConnectionQualityChanged(LiveKit.Proto.ConnectionQuality quality, Participant participant)
        {
            if (participant is not RemoteParticipant remote)
                return;
            foreach (var publication in remote.Tracks.Values)
                SetLoss(publication, StreamFeedLoss.PresenterLost, quality == LiveKit.Proto.ConnectionQuality.QualityLost);
        }

        private void OnReconnecting(Room room) => SetRoomLoss(true);

        private void OnReconnected(Room room) => SetRoomLoss(false);

        private void SetRoomLoss(bool lost)
        {
            foreach (var feed in _feeds.Values)
                feed.SetLoss(StreamFeedLoss.RoomReconnecting, lost);
        }

        private void SetLoss(TrackPublication publication, StreamFeedLoss loss, bool lost)
        {
            if (publication.Kind == LiveKit.Proto.TrackKind.KindVideo && _feeds.TryGetValue(publication.Name, out var feed))
                feed.SetLoss(loss, lost);
        }

        private void OnDisconnected(Room room)
        {
            if (room == _room)
                Leave();
        }
    }

    public readonly struct StreamRoomSource
    {
        public string Source { get; }
        public string Name { get; }

        public StreamRoomSource(string source, string name)
        {
            Source = source;
            Name = name;
        }
    }
}
