// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Networking.Players;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Presence;
using EDIVE.ServiceHub.Streaming.Displays;
using EDIVE.ServiceHub.Streaming.Sources;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming
{
    // Displays and LiveKit for this app, controlled by ServiceHub through presence
    public class StreamingManager : ALoadableServiceBehaviour<StreamingManager>
    {
        [SerializeField]
        [Required]
        private ViewStreamSource _ViewSource;

        private readonly List<StreamScreen> _screens = new();

        private PresenceService _presence;
        private bool _displaysDirty;

        public StreamRoom Room { get; private set; }
        public ViewShare ViewShare { get; private set; }
        public IReadOnlyList<StreamScreen> Screens => _screens;

        [ShowInInspector, ReadOnly]
        public bool IsAvailable => _presence != null && _presence.IsEnabled;

        // A display was added, removed or shows something else
        public event Action<StreamingManager> ScreensChanged;

        // Someone wants a display this user presents on
        public event Action<DisplayHandoverRequest> HandoverRequested;

        // Text from ServiceHub, for example a display taken over
        public event Action<string, string> NoticeReceived;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            if (AppCore.Services.TryGet<ServiceHubManager>(out var serviceHub))
                _presence = serviceHub.Presence;

            Room = new StreamRoom(_presence, this);
            ViewShare = new ViewShare(Room, _presence, _ViewSource, this) { LabelProvider = GetViewLabel };
            if (_presence == null)
                return UniTask.CompletedTask;

            _presence.RegisterHandler(PresenceCommandType.DISPLAY_SHOW, OnDisplayShow);
            _presence.RegisterHandler(PresenceCommandType.DISPLAY_CLEAR, OnDisplayClear);
            _presence.RegisterHandler(PresenceCommandType.DISPLAY_HANDOVER, OnDisplayHandover);
            _presence.RegisterHandler(PresenceCommandType.NOTICE, OnNotice);
            _presence.HostStarted += OnHostStarted;
            MarkDisplaysDirty();
            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(ServiceHubManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ViewShare?.Dispose();
            Room?.Dispose();
            if (_presence == null)
                return;
            _presence.UnregisterHandler(PresenceCommandType.DISPLAY_SHOW, OnDisplayShow);
            _presence.UnregisterHandler(PresenceCommandType.DISPLAY_CLEAR, OnDisplayClear);
            _presence.UnregisterHandler(PresenceCommandType.DISPLAY_HANDOVER, OnDisplayHandover);
            _presence.UnregisterHandler(PresenceCommandType.NOTICE, OnNotice);
            _presence.HostStarted -= OnHostStarted;
        }

        public void RegisterScreen(StreamScreen screen)
        {
            if (_screens.Contains(screen))
                return;
            _screens.Add(screen);
            screen.StateChanged += OnScreenStateChanged;
            MarkDisplaysDirty();
        }

        public void UnregisterScreen(StreamScreen screen)
        {
            if (!_screens.Remove(screen))
                return;
            screen.StateChanged -= OnScreenStateChanged;
            MarkDisplaysDirty();
        }

        public bool TryGetScreen(string id, out StreamScreen screen)
        {
            screen = _screens.FirstOrDefault(s => s.Id == id);
            return screen != null;
        }

        // Goes through ServiceHub like on the web: follows the display's rule and asks a presenter to hand over
        public async UniTask<StreamActionResponse> SetDisplayAsync(StreamScreen screen, string source, CancellationToken cancellationToken = default)
        {
            if (screen == null || _presence == null)
                return new StreamActionResponse { Result = PresenceResult.FAILED };
            var response = await _presence.SetDisplayAsync(screen.Id, source, cancellationToken);
            if (response.IsSuccess && response.Result != null)
                return response.Result;
            return new StreamActionResponse { Result = PresenceResult.FAILED, Message = response.ErrorMessage };
        }

        // Shares my view with everyone first, the display is seen by the whole room
        public async UniTask<StreamActionResponse> ShowMyViewAsync(StreamScreen screen, CancellationToken cancellationToken = default)
        {
            if (ViewShare == null || !await ViewShare.StartAsync(cancellationToken))
                return new StreamActionResponse { Result = PresenceResult.FAILED };
            return await SetDisplayAsync(screen, ViewShare.SourceId, cancellationToken);
        }

        private void LateUpdate()
        {
            if (!_displaysDirty)
                return;
            _displaysDirty = false;
            ReportDisplays();
        }

        private void MarkDisplaysDirty()
        {
            _displaysDirty = true;
            ScreensChanged?.Invoke(this);
        }

        private static string GetViewLabel()
        {
            var name = AppCore.Services.TryGet<NetworkPlayerManager>(out var players) ? players.LocalPlayer?.AuthUserInfo?.Name : null;
            return string.IsNullOrEmpty(name) ? "View" : $"{name}'s view";
        }

        private void OnScreenStateChanged(StreamScreen screen) => MarkDisplaysDirty();

        private void OnHostStarted(PresenceClient host) => MarkDisplaysDirty();

        // Players report the displays only they see, the host the shared ones
        private void ReportDisplays()
        {
            if (_presence == null)
                return;
            _presence.Player?.SetDisplays(_screens.Where(s => !s.IsShared).Select(s => s.ToPresence()));
            _presence.Host?.SetDisplays(_screens.Where(s => s.IsShared).Select(s => s.ToPresence()));
        }

        private UniTask<PresenceReply> OnDisplayShow(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            var state = new StreamScreenState(command.GetString("source"), command.GetString("presenter"), command.GetString("presenter_name"));
            return UniTask.FromResult(ApplyDisplay(client, command.GetString("display_id"), state));
        }

        private UniTask<PresenceReply> OnDisplayClear(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(ApplyDisplay(client, command.GetString("display_id"), StreamScreenState.Empty));
        }

        private async UniTask<PresenceReply> OnDisplayHandover(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            if (client.IsHost || HandoverRequested == null)
                return null;

            var displayName = TryGetScreen(command.GetString("display_id"), out var screen) ? screen.Name : null;
            var request = new DisplayHandoverRequest(command, displayName);
            HandoverRequested?.Invoke(request);
            try
            {
                var remaining = request.Remaining;
                var (isAnswer, keep) = await UniTask.WhenAny(
                    request.Answer,
                    UniTask.Delay(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, true, cancellationToken: cancellationToken));
                // No answer hands it over on ServiceHub's side
                if (!isAnswer)
                    return null;
                return new PresenceReply(keep ? PresenceResult.KEEP : PresenceResult.HANDOVER);
            }
            finally
            {
                request.Close();
            }
        }

        private UniTask<PresenceReply> OnNotice(PresenceClient client, PresenceCommand command, CancellationToken cancellationToken)
        {
            if (!client.IsHost)
                NoticeReceived?.Invoke(command.GetString("text"), command.GetString("kind"));
            return UniTask.FromResult<PresenceReply>(null);
        }

        private PresenceReply ApplyDisplay(PresenceClient client, string displayId, StreamScreenState state)
        {
            // Each presence owns its own displays only
            if (!TryGetScreen(displayId, out var screen) || screen.IsShared != client.IsHost)
                return new PresenceReply(PresenceResult.FAILED);
            if (!state.IsEmpty && screen.Allow == StreamScreenAllow.Nobody)
                return new PresenceReply(PresenceResult.FAILED);
            if (!screen.Show(state))
                return new PresenceReply(PresenceResult.FAILED);

            ReportDisplays();
            return new PresenceReply(PresenceResult.APPLIED);
        }
    }
}
