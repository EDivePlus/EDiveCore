// Author: František Holubec
// Created: 05.02.2026

using System;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Outputs;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Audio.Replay
{
    public class VoiceChatReplayProxy : NetworkBehaviour
    {
        [SerializeField]
        private StreamedAudioSourceOutput _AudioOutput;
        
        public StreamedAudioSourceOutput AudioOutput => _AudioOutput;
        public AudioSource UnityAudioSource => _AudioOutput.Stream.UnityAudioSource;
        public event Action<AudioFrame> AudioFrameReceived;
        
        private VoiceChatPlayerController _voiceChatController;
        private EnhancedStreamedAudioSourceOutput _chatAudioOutput;
        
        private void Awake()
        {
            _voiceChatController = GetComponentInParent<VoiceChatPlayerController>();
            if (_voiceChatController == null)
            {
                Debug.LogError($"No VoiceChatPlayerController found for {gameObject.name}", this);
            }
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            if (_voiceChatController != null)
            {
                _voiceChatController.AudioOutputPromise.Then(output =>
                {
                    _chatAudioOutput = output;
                    _chatAudioOutput.AudioFrameReceived += OnOutputAudioFrameReceived;
                });
            }
        }
        
        public override void OnStopClient()
        {
            base.OnStopClient();
            if (_chatAudioOutput != null)
            {
                _chatAudioOutput.AudioFrameReceived -= OnOutputAudioFrameReceived;
            }
        }
        
        private void OnOutputAudioFrameReceived(AudioFrame audioFrame)
        {
            AudioFrameReceived?.Invoke(audioFrame);
        }
        
        public void FeedAudioFrame(AudioFrame frame)
        {
            _AudioOutput.Feed(frame);
        }
    }
}
