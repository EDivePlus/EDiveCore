// Author: František Holubec
// Created: 17.02.2026

using Adrenak.UniVoice;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    [RequireComponent(typeof(ReplayAudioOutput))]
    public class NetworkReplayAudioOutput : NetworkBehaviour
    {
        private ReplayAudioOutput _replayAudioOutput;

        private void Awake()
        {
            _replayAudioOutput = GetComponent<ReplayAudioOutput>();
        }

        public override void OnStartServer()
        {
            _replayAudioOutput.FedAudioFrame += OnFedAudioFrame;
            _replayAudioOutput.PlayBackEnabledChanged += OnPlaybackEnabledChanged;
        }

        public override void OnStopServer()
        {
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
        
        [ObserversRpc(ExcludeServer = true)]
        private void ObserversFeedAudioFrame(AudioFrame frame)
        {
            _replayAudioOutput.Feed(frame);
        }
        
        [ObserversRpc(ExcludeServer = true)]
        private void ObserversSetPlaybackEnabled(bool isEnabled)
        {
            _replayAudioOutput.SetPlaybackEnabled(isEnabled);
        }
    }
}
