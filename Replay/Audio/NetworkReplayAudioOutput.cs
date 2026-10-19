// Author: František Holubec
// Created: 17.02.2026

using Adrenak.UniVoice;
using PurrNet;
using UnityEngine;

namespace EDIVE.Replay.Audio
{
    [RequireComponent(typeof(ReplayAudioOutput))]
    public class NetworkReplayAudioOutput : NetworkBehaviour
    {
        private ReplayAudioOutput _replayAudioOutput;

        private void Awake()
        {
            _replayAudioOutput = GetComponent<ReplayAudioOutput>();
        }

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer) return;
            _replayAudioOutput.FedAudioFrame += OnFedAudioFrame;
            _replayAudioOutput.PlayBackEnabledChanged += OnPlaybackEnabledChanged;
            _replayAudioOutput.BufferCleared += OnBufferCleared;
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer) return;
            _replayAudioOutput.FedAudioFrame -= OnFedAudioFrame;
            _replayAudioOutput.PlayBackEnabledChanged -= OnPlaybackEnabledChanged;
            _replayAudioOutput.BufferCleared -= OnBufferCleared;
        }

        private void OnBufferCleared()
        {
            ObserversClearBuffer();
        }

        [ObserversRpc]
        private void ObserversClearBuffer()
        {
            if (isServer) return;
            _replayAudioOutput.ClearBuffer();
        }

        private void OnFedAudioFrame(AudioFrame frame)
        {
            ObserversFeedAudioFrame(frame);
        }

        private void OnPlaybackEnabledChanged(bool isEnabled)
        {
            ObserversSetPlaybackEnabled(isEnabled);
        }

        // Host already applied it locally, skip to avoid echo and double playback
        [ObserversRpc]
        private void ObserversFeedAudioFrame(AudioFrame frame)
        {
            if (isServer) return;
            _replayAudioOutput.Feed(frame);
        }

        [ObserversRpc]
        private void ObserversSetPlaybackEnabled(bool isEnabled)
        {
            if (isServer) return;
            _replayAudioOutput.SetPlaybackEnabled(isEnabled);
        }
    }
}
