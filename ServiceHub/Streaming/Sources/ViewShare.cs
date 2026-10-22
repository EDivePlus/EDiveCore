// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.ServiceHub.Presence;
using LiveKit;
using LiveKit.Proto;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Sources
{
    // Publishes this user's view as view:<participant>, open to everyone or only to who asked
    public class ViewShare
    {
        private const float JOIN_TIMEOUT = 20f;

        private readonly StreamRoom _room;
        private readonly PresenceService _presence;
        private readonly ViewStreamSource _source;
        private readonly MonoBehaviour _runner;
        private readonly HashSet<string> _allowed = new();
        private readonly List<StreamShareRequest> _requests = new();
        private readonly List<StreamViewer> _viewers = new();

        private TextureVideoSource _videoSource;
        private LocalVideoTrack _videoTrack;
        private Coroutine _videoUpdate;
        private bool _starting;
        private ViewShareAudio _audio;

        public bool IsSharing => _videoTrack != null;
        public bool IsOpen { get; private set; }
        public string SourceId { get; private set; }
        public IReadOnlyCollection<string> Allowed => _allowed;

        public bool IncludeMenus => _source == null || _source.IncludeMenus;

        // My own track never comes back from LiveKit, displays showing it use this
        public Texture LocalTexture => IsSharing ? _source.Output : null;
        public bool LocalUpsideDown => _source != null && !_source.FlipVertically;

        // Sound for web viewers, both off by default
        public bool IncludeHearing { get; private set; }
        public bool IncludeMic { get; private set; }
        public IReadOnlyList<StreamShareRequest> Requests => _requests;

        // From ServiceHub, the owner always sees who watches
        public IReadOnlyList<StreamViewer> Viewers => _viewers;
        public Func<string> LabelProvider { get; set; }

        public event Action<ViewShare> Changed;
        public event Action<StreamShareRequest> Requested;

        internal ViewShare(StreamRoom room, PresenceService presence, ViewStreamSource source, MonoBehaviour runner)
        {
            _room = room;
            _presence = presence;
            _source = source;
            _runner = runner;
            _room.ConnectionChanged += OnConnectionChanged;

            if (_presence == null)
                return;
            _presence.RegisterHandler(PresenceCommandType.SHARE_REQUEST, OnShareRequest);
            _presence.RegisterHandler(PresenceCommandType.SHARE_ALLOW, OnShareAllow);
            _presence.RegisterHandler(PresenceCommandType.SHARE_STOP, OnShareStop);
            _presence.RegisterHandler(PresenceCommandType.SHARE_VIEWERS, OnShareViewers);
        }

        internal void Dispose()
        {
            Stop();
            _room.ConnectionChanged -= OnConnectionChanged;
            if (_presence == null)
                return;
            _presence.UnregisterHandler(PresenceCommandType.SHARE_REQUEST, OnShareRequest);
            _presence.UnregisterHandler(PresenceCommandType.SHARE_ALLOW, OnShareAllow);
            _presence.UnregisterHandler(PresenceCommandType.SHARE_STOP, OnShareStop);
            _presence.UnregisterHandler(PresenceCommandType.SHARE_VIEWERS, OnShareViewers);
        }

        // Started by the user: everyone in the room may watch
        public UniTask<bool> StartAsync(CancellationToken cancellationToken = default) => StartAsync(true, null, cancellationToken);

        public async UniTask<bool> StartAsync(bool open, IEnumerable<string> allowed, CancellationToken cancellationToken = default)
        {
            if (IsSharing)
            {
                if (open)
                    IsOpen = true;
                AddAllowed(allowed);
                ApplyPermissions();
                return true;
            }
            if (_starting || _source == null)
                return false;

            _starting = true;
            _room.Hold();
            try
            {
                if (!await _room.WaitConnectedAsync(JOIN_TIMEOUT, cancellationToken))
                {
                    _room.Unhold();
                    return false;
                }

                var room = _room.Room;
                SourceId = $"view:{_room.Identity}";
                _videoSource = new TextureVideoSource(_source.Begin()) { FlipVertically = _source.FlipVertically };
                _videoTrack = LocalVideoTrack.CreateVideoTrack(SourceId, _videoSource, room);
                var options = new TrackPublishOptions
                {
                    Source = TrackSource.SourceScreenshare,
                    VideoEncoding = new VideoEncoding { MaxBitrate = 2_500_000, MaxFramerate = 30 }
                };

                // Permissions go first so a request-only share is never open, even briefly
                IsOpen = open;
                _allowed.Clear();
                if (allowed != null)
                    _allowed.UnionWith(allowed.Where(i => !string.IsNullOrEmpty(i)));
                ApplyPermissions();

                var publish = room.LocalParticipant.PublishTrack(_videoTrack, options);
                try
                {
                    await publish.AsUniTask(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await publish.AsUniTask();
                    if (!publish.IsError)
                        room.LocalParticipant.UnpublishTrack(_videoTrack, true);
                    throw;
                }
                if (publish.IsError)
                {
                    Debug.LogWarning("[ViewShare] Publish failed.");
                    Cleanup();
                    return false;
                }

                _videoSource.Start();
                _videoUpdate = _runner.StartCoroutine(_videoSource.Update());
                ReportSource();
                _audio = new ViewShareAudio(room, _room.Identity);
                await ApplyAudioAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                Cleanup();
                return false;
            }
            finally
            {
                _starting = false;
            }
        }

        public void SetMenus(bool includeMenus)
        {
            if (_source == null || _source.IncludeMenus == includeMenus)
                return;
            _source.IncludeMenus = includeMenus;
            if (_videoSource != null)
                _videoSource.FlipVertically = _source.FlipVertically;
            Changed?.Invoke(this);
        }

        public void SetAudio(bool includeHearing, bool includeMic)
        {
            IncludeHearing = includeHearing;
            IncludeMic = includeMic;
            if (IsSharing)
                ApplyAudioAsync(CancellationToken.None).Forget();
            else
                Changed?.Invoke(this);
        }

        public void Stop()
        {
            if (!IsSharing)
                return;
            var room = _room.Room;
            if (room != null)
                room.LocalParticipant.UnpublishTrack(_videoTrack, true);
            Cleanup();
        }

        // Lets one more person watch a share started by a request
        public void AllowViewer(string identity)
        {
            if (!IsSharing || string.IsNullOrEmpty(identity))
                return;
            AddAllowed(new[] { identity });
            ApplyPermissions();
        }

        private async UniTask ApplyAudioAsync(CancellationToken cancellationToken)
        {
            if (_audio == null)
                return;
            _audio.SetMic(IncludeMic);
            await _audio.SetHearingAsync(IncludeHearing, cancellationToken);
            ReportSource();
        }

        private void AddAllowed(IEnumerable<string> identities)
        {
            if (identities == null)
                return;
            foreach (var identity in identities.Where(i => !string.IsNullOrEmpty(i)))
                _allowed.Add(identity);
            ReportSource();
        }

        private void Cleanup()
        {
            if (_videoUpdate != null && _runner != null)
                _runner.StopCoroutine(_videoUpdate);
            _videoUpdate = null;
            _audio?.Dispose();
            _audio = null;
            _videoSource?.Stop();
            _videoSource?.Dispose();
            _videoSource = null;
            _videoTrack = null;
            _source.End();
            _allowed.Clear();
            _viewers.Clear();
            IsOpen = false;
            SourceId = null;
            ApplyPermissions();
            _room.Unhold();
            ReportSource();
        }

        private void ApplyPermissions()
        {
            var room = _room.Room;
            if (room == null)
                return;

            try
            {
                var permissions = IsOpen ? null : _allowed.Select(identity => new ParticipantTrackPermission { ParticipantIdentity = identity, AllowAll = true });
                room.LocalParticipant.SetTrackSubscriptionPermissions(IsOpen, permissions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            ReportSource();
        }

        private void ReportSource()
        {
            if (_presence?.Player == null)
                return;

            var sources = new List<PresenceSource>();
            if (IsSharing)
            {
                sources.Add(new PresenceSource
                {
                    Id = SourceId,
                    Label = LabelProvider?.Invoke(),
                    HasAudio = IncludeHearing || IncludeMic,
                    Open = IsOpen,
                    Allowed = IsOpen ? null : _allowed.ToList()
                });
            }
            _presence.Player.SetSources(sources);
            Changed?.Invoke(this);
        }

        private void OnConnectionChanged(StreamRoom room)
        {
            // Left the room or changed session, the track is gone
            if (!room.IsConnected && IsSharing)
                Cleanup();
        }

        private async UniTask<PresenceReply> OnShareRequest(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            if (client.IsHost)
                return new PresenceReply(PresenceResult.UNSUPPORTED);
            // Nobody to ask, no reply counts as Deny
            if (Requested == null)
                return new PresenceReply(PresenceResult.DENY);

            var request = new StreamShareRequest(command);
            _requests.Add(request);
            Requested?.Invoke(request);
            Changed?.Invoke(this);

            bool allowed;
            try
            {
                var remaining = request.Remaining;
                var (isAnswer, answer) = await UniTask.WhenAny(
                    request.Answer,
                    UniTask.Delay(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, true, cancellationToken: cancellationToken));
                allowed = isAnswer && answer;
            }
            finally
            {
                _requests.Remove(request);
                request.Close();
                Changed?.Invoke(this);
            }

            if (!allowed)
                return new PresenceReply(PresenceResult.DENY);
            SetMenus(request.IncludeMenus);
            SetAudio(request.IncludeHearing, request.IncludeMic);
            // Reply before the countdown ends, publishing follows
            StartAsync(false, new[] { request.From }, cancellationToken).Forget();
            return new PresenceReply(PresenceResult.ALLOW);
        }

        private UniTask<PresenceReply> OnShareAllow(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            if (!IsSharing)
                return UniTask.FromResult(new PresenceReply(PresenceResult.FAILED));
            AllowViewer(command.GetString("identity"));
            return UniTask.FromResult(new PresenceReply(PresenceResult.APPLIED));
        }

        private UniTask<PresenceReply> OnShareViewers(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            if (command.GetString("source") == SourceId)
            {
                _viewers.Clear();
                if (command.Data?["viewers"] is JArray viewers)
                {
                    foreach (var viewer in viewers.OfType<JObject>())
                        _viewers.Add(new StreamViewer(viewer.Value<string>("identity"), viewer.Value<string>("name"), viewer.Value<string>("kind")));
                }
                Changed?.Invoke(this);
            }
            return UniTask.FromResult(new PresenceReply(PresenceResult.APPLIED));
        }

        private UniTask<PresenceReply> OnShareStop(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            Stop();
            return UniTask.FromResult(new PresenceReply(PresenceResult.APPLIED));
        }
    }
}
