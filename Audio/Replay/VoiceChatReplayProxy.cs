// Author: František Holubec
// Created: 05.02.2026

using System;
using System.Collections.Generic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using EDIVE.Core;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    public class VoiceChatReplayProxy : NetworkBehaviour
    {
        [SerializeField]
        public BufferedAudioOutput _AudioOutput;
        public BufferedAudioOutput AudioOutput => _AudioOutput;
        
        public event Action<AudioFrame> AudioFrameReceived;

        private List<IAudioFilter> _encodeFilters;
        private List<IAudioFilter> _decodeFilters;
        
        public override void OnStartServer()
        {
            if (!AppCore.Services.TryGet<AudioManager>(out var audioManager)) 
                return;
            
            audioManager.ServerReceivedPeerAudioFrame += OnReceivedPeerAudioFrame;
        }

        public override void OnStopServer()
        {
            if (AppCore.Services.TryGet<AudioManager>(out var audioManager))
            {
                audioManager.ServerReceivedPeerAudioFrame -= OnReceivedPeerAudioFrame;
            }
        }

        public override void OnStartClient()
        {
            if (!AppCore.Services.TryGet<AudioManager>(out var audioManager)) 
                return;
            
            _decodeFilters = new List<IAudioFilter>
            {
                new ConcentusDecodeFilter()
            };

            if (NetworkObject.IsOwner)
            {
                _encodeFilters = new List<IAudioFilter>
                {
                    new RNNoiseFilter(),
                    new SimpleVadFilter(new SimpleVad()),
                    new ConcentusEncodeFilter()
                };
                audioManager.LocalAudioFrameReady += OnReceivedLocalAudioFrame;
            }
            else
            {
                audioManager.ClientReceivedPeerAudioFrame += OnReceivedPeerAudioFrame;
            }
        }

        public override void OnStopClient()
        {
            if (AppCore.Services.TryGet<AudioManager>(out var audioManager))
            {
                audioManager.LocalAudioFrameReady -= OnReceivedLocalAudioFrame;
                audioManager.ClientReceivedPeerAudioFrame -= OnReceivedPeerAudioFrame;
            }
        }
        
        private void OnReceivedPeerAudioFrame(int clientID, AudioFrame audioFrame)
        {
            if (clientID != NetworkObject.OwnerId)
                return;

            ReceiveAudioFrame(audioFrame);
        }
        
        private void OnReceivedLocalAudioFrame(AudioFrame audioFrame)
        {
            if (TryProcessAudioFrame(ref audioFrame, _encodeFilters))
                ReceiveAudioFrame(audioFrame);
        }
        
        private static bool TryProcessAudioFrame(ref AudioFrame frame, List<IAudioFilter> filters)
        {
            if (filters != null) {
                foreach (var filter in filters) {
                    frame = filter.Run(frame);
                    if (frame.samples == null)
                        break;
                }
            }

            return frame.samples != null && frame.samples.Length > 0;
        }
        
        private void ReceiveAudioFrame(AudioFrame audioFrame)
        {
            AudioFrameReceived?.Invoke(audioFrame);
        }
        
        public void FeedAudioFrame(AudioFrame frame)
        {
            if (IsServerStarted)
            {
                ClientFeedAudioFrame(frame);
            }
            else
            {
                FeedAudioFrameInternal(frame);
            }
        }

        [ObserversRpc]
        private void ClientFeedAudioFrame(NetAudioFrame frame)
        {
            FeedAudioFrameInternal(frame);
        }
        
        [Client]
        private void FeedAudioFrameInternal(AudioFrame frame)
        {
            if (TryProcessAudioFrame(ref frame, _decodeFilters)) 
                _AudioOutput.Feed(frame.frequency, frame.channelCount, frame.samples, frame.timestamp);
        }

        public void StopAudioOutput()
        {
            _AudioOutput.Stop();
        }
    }
}
