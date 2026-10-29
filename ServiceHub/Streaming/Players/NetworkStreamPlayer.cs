// Author: František Holubec
// Created: 21.10.2026

using System;
using EDIVE.Core;
using EDIVE.Networking.Players;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Presence;
using EDIVE.ServiceHub.Streaming.Sources;
using PurrNet;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Players
{
    // Shares the player's ServiceHub identity and view share with the session, for the Streams list
    [RequireComponent(typeof(NetworkPlayerController))]
    public class NetworkStreamPlayer : NetworkBehaviour
    {
        private readonly SyncVar<StreamPlayerState> _state = new(StreamPlayerState.Empty, ownerAuth: true);

        private NetworkPlayerController _player;
        private IDisposable _streamingRegistration;
        private IDisposable _serviceHubRegistration;
        private ViewShare _viewShare;
        private PresenceService _presence;

        public NetworkPlayerController Player => _player ? _player : _player = GetComponent<NetworkPlayerController>();
        public StreamPlayerState State => _state.value ?? StreamPlayerState.Empty;

        public event Action<NetworkStreamPlayer> Changed;

        protected override void OnSpawned(bool asServer)
        {
            if (asServer)
                return;
            _state.onChanged += OnStateChanged;
            if (!isOwner)
                return;
            _streamingRegistration = AppCore.Services.WhenRegistered<StreamingManager>(OnStreamingRegistered);
            _serviceHubRegistration = AppCore.Services.WhenRegistered<ServiceHubManager>(OnServiceHubRegistered);
        }

        protected override void OnDespawned(bool asServer)
        {
            if (asServer)
                return;
            _state.onChanged -= OnStateChanged;
            _streamingRegistration?.Dispose();
            _streamingRegistration = null;
            _serviceHubRegistration?.Dispose();
            _serviceHubRegistration = null;
            BindViewShare(null);
            BindPresence(null);
        }

        private void OnStreamingRegistered(StreamingManager streaming)
        {
            BindViewShare(streaming.ViewShare);
            Publish();
        }

        private void OnServiceHubRegistered(ServiceHubManager serviceHub)
        {
            BindPresence(serviceHub.Presence);
            Publish();
        }

        private void BindViewShare(ViewShare viewShare)
        {
            if (_viewShare != null)
                _viewShare.Changed -= OnViewShareChanged;
            _viewShare = viewShare;
            if (_viewShare != null)
                _viewShare.Changed += OnViewShareChanged;
        }

        private void BindPresence(PresenceService presence)
        {
            if (_presence?.Player != null)
                _presence.Player.Reported -= OnPresenceReported;
            _presence = presence;
            if (_presence?.Player != null)
                _presence.Player.Reported += OnPresenceReported;
        }

        private void OnViewShareChanged(ViewShare _) => Publish();
        private void OnPresenceReported(PresenceClient _) => Publish();

        private void Publish()
        {
            if (!isSpawned || !isOwner)
                return;
            var sharing = _viewShare != null && _viewShare.IsSharing;
            var state = new StreamPlayerState(_presence?.Player?.Identity, sharing ? _viewShare.SourceId : null, sharing && _viewShare.IsOpen);
            if (!state.Matches(_state.value))
                _state.value = state;
        }

        private void OnStateChanged(StreamPlayerState _) => Changed?.Invoke(this);
    }
}
