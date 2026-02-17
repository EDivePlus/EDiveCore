// Author: František Holubec
// Created: 17.02.2026

using System;
using System.Collections.Generic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using FishNet;
using FishNet.Managing;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    [RequireComponent(typeof(BufferedAudioOutput))]
    public class ReplayAudioOutput : MonoBehaviour
    {
        private BufferedAudioOutput _bufferedAudioOutput;
        private NetworkManager _networkManager;
        private List<IAudioFilter> _decodeFilters;

        public int InitialBufferSize => _bufferedAudioOutput.InitialBufferSize;
        public bool IsPlaybackEnabled => _bufferedAudioOutput.PlaybackEnabled;
        
        public event Action<bool> PlayBackEnabledChanged;
        public event Action<AudioFrame> FedAudioFrame;
        
        private void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;
            _bufferedAudioOutput = GetComponent<BufferedAudioOutput>();
        }

        public void Feed(AudioFrame frame)
        {
            FedAudioFrame?.Invoke(frame);
            if (_networkManager != null && !_networkManager.IsClientStarted)
                return;
            
            _decodeFilters ??= new List<IAudioFilter>
            {
                new ConcentusDecodeFilter()
            };
            
            if (AudioUtils.TryProcessAudioFrame(ref frame, _decodeFilters)) 
                _bufferedAudioOutput.Feed(frame);
        }

        public void SetPlaybackEnabled(bool state)
        {
            if (state == IsPlaybackEnabled)
                return;
            
            _bufferedAudioOutput.SetPlaybackEnabled(state);
            PlayBackEnabledChanged?.Invoke(state);
        }
    }
}
