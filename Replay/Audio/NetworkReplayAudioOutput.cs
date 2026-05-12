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
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer) return;
            _replayAudioOutput.FedAudioFrame -= OnFedAudioFrame;
            _replayAudioOutput.PlayBackEnabledChanged -= OnPlaybackEnabledChanged;
        }

        private void OnFedAudioFrame(AudioFrame frame)
        {
            ObserversFeedAudioFrame(frame);
        }

        private void OnPlaybackEnabledChanged(bool isEnabled)
        {
            ObserversSetPlaybackEnabled(isEnabled);
        }

        // PurrNet doesn't have FishNet's ExcludeServer flag — by default the server doesn't run
        // ObserversRpc bodies locally (runLocally defaults to false), so the host's server side
        // is already excluded. The host's client side still receives the call as an observer.
        [ObserversRpc]
        private void ObserversFeedAudioFrame(AudioFrame frame)
        {
            _replayAudioOutput.Feed(frame);
        }

        [ObserversRpc]
        private void ObserversSetPlaybackEnabled(bool isEnabled)
        {
            _replayAudioOutput.SetPlaybackEnabled(isEnabled);
        }
    }
}
