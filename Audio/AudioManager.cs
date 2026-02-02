// Author: František Holubec
// Created: 02.02.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.NativeUtils;
using EDIVE.Networking.Players;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Audio
{
    public class AudioManager : ALoadableServiceBehaviour<AudioManager>
    {
        [SerializeField]
        [SuffixLabel("ms", true)]
        private int _MicFrameDuration = 60;
        
        [SerializeField]
        private float _VoiceChatDistance = 25f;
        
        private ClientSession<int> _voiceChatSession;
        private bool _microphonePermissionGranted = false;
        
        private readonly List<object> _voiceChatMuteRequests = new();
        public bool VoiceChatMuted => _voiceChatMuteRequests.Any();
        
        public bool EnableSpatialAudio
        {
            get => PlayerPrefs.GetInt("Audio_SpatialAudio", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("Audio_SpatialAudio", value ? 1 : 0);
                RefreshSpatialAudio();
            }
        }

        public bool AllowMic
        {
            get => PlayerPrefs.GetInt("Audio_AllowMic", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("Audio_AllowMic", value ? 1 : 0);
                RefreshMicrophone();
            }
        }

        public string CurrentMicrophoneName
        {
            get => PlayerPrefs.GetString("Audio_MicName", string.Empty);
            private set => PlayerPrefs.SetString("Audio_MicName", value);
        }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            InitializeMicrophone();
            InitializeVoiceChat();
            
            return UniTask.CompletedTask;
        }

        private void InitializeMicrophone()
        {
            if (!PlatformUtils.IsHeadless())
            {
                Mic.Init();
            }
            
            AudioUtils.CheckMicrophonePermission(micPermissionGranted =>
            {
                _microphonePermissionGranted = micPermissionGranted;
                if (_voiceChatSession != null && micPermissionGranted && _voiceChatSession.Input is not UniMicInput)
                {
                    _voiceChatSession.Input = ResolveAndCreateAudioInput();
                }
            });
        }

        private void InitializeVoiceChat()
        {
            if (!PlatformUtils.IsHeadless())
            {
                InitializeClient();
            }
            InitializeServer();
        }
      
        private void InitializeClient()
        {
            var client = new FishNetClient();
            Mic.Init();
            
            var audioInput = ResolveAndCreateAudioInput();
            _voiceChatSession = new ClientSession<int>(client, audioInput, () =>
            {
                var audioOutput = StreamedAudioSourceOutput.New();
                audioOutput.Stream.TargetLatency = 0.3f;
                audioOutput.Stream.PitchMaxCorrection = 0.05f;
                audioOutput.Stream.PitchProportionalGain = 0.2f;
                audioOutput.Stream.DownwardPitchCorrectionScale = 1f;
                return audioOutput;
            });
            _voiceChatSession.InputFilters.Add(new RNNoiseFilter()); // Noise suppression
            _voiceChatSession.InputFilters.Add(new SimpleVadFilter(new SimpleVad())); // Voice activity detection
            _voiceChatSession.InputFilters.Add(new ConcentusEncodeFilter()); // Opus encoding
            _voiceChatSession.AddOutputFilter<ConcentusDecodeFilter>(() => new ConcentusDecodeFilter()); // Opus decoding
            
            client.OnJoined += OnVoiceChatClientJoined;
            client.OnLeft += OnVoiceChatClientLeft;
            client.OnPeerJoined += OnVoiceChatPeerJoined;
            client.OnPeerLeft += OnVoiceChatPeerLeft;
        }
        
        private void OnVoiceChatClientLeft()
        {
            Debug.Log("[AudioManager] You left the chatroom");
        }
        
        private void OnVoiceChatClientJoined(int id, List<int> peerIds)
        {
            Debug.Log($"[AudioManager] You are Peer ID {id} your peers are {string.Join(", ", peerIds)}");
        }

        private void OnVoiceChatPeerLeft(int id)
        {
            Debug.Log($"[AudioManager] Peer '{id}' left the chatroom");
        }

        private void OnVoiceChatPeerJoined(int id)
        {
            Debug.Log($"[AudioManager] Peer '{id}' joined the chatroom");

            var output = _voiceChatSession.PeerOutputs[id] as StreamedAudioSourceOutput;
            if (output == null)
            {
                Debug.LogError($"[AudioManager] Could not get StreamedAudioSourceOutput for peer {id}");
                return;
            }
            output.gameObject.name = $"StreamedAudioOutput ({id})";
            InitializePeerSpatialAudioAsync(id, output).Forget();
        }
        
        private async UniTask InitializePeerSpatialAudioAsync(int id, StreamedAudioSourceOutput output)
        {
            var playerManager = AppCore.Services.Get<NetworkPlayerManager>();
            var playerController = await playerManager.AwaitPlayerController(id);

            if (playerController == null)
            {
                Debug.LogWarning($"[AudioManager] Could not find player controller for peer {id}");
                return;
            }

            if (!playerController.TryGetComponent<VoiceChatPlayerController>(out var peerAvatar))
            {
                Debug.LogWarning($"[AudioManager] Player controller for peer {id} does not have a VoiceChatPlayerController component");
                return;
            }

            var audioSource = output.Stream.UnityAudioSource;
            audioSource.transform.SetParent(peerAvatar.PeerRoot); // Parent the AudioSource to the avatar
            audioSource.transform.localPosition = Vector3.zero; // Reset position

            audioSource.spatialBlend = EnableSpatialAudio ? 1 : 0; 
            audioSource.maxDistance = _VoiceChatDistance;
            Debug.Log($"[AudioManager] AudioSource of player '{id}' ");
        }

        private void InitializeServer()
        {
            var server = new FishNetServer();
            server.OnServerStart += OnVoiceChatServerStarted;
            server.OnServerStop += OnVoiceChatServerStopped;
        }
        
        private void OnVoiceChatServerStarted()
        {
            Debug.Log("[AudioManager] Voice chat server started");
        }

        private void OnVoiceChatServerStopped()
        {
            Debug.Log("[AudioManager] Voice chat server stopped");
        }

        public void AddVoiceChatMuteRequest(object requester)
        {
            if (!_voiceChatMuteRequests.Contains(requester))
            {
                _voiceChatMuteRequests.Add(requester);
            }
            // TODO mute
        }
        
        public void RemoveVoiceChatMuteRequest(object requester)
        {
            if (_voiceChatMuteRequests.Contains(requester))
            {
                _voiceChatMuteRequests.Remove(requester);
            }
            // TODO unmute if no more requests
        }
        
        public void RefreshSpatialAudio()
        {
            if (_voiceChatSession == null)
                return;

            foreach (var output in _voiceChatSession.PeerOutputs.Values)
            {
                if (output is not StreamedAudioSourceOutput streamedOutput)
                    continue;

                streamedOutput.Stream.UnityAudioSource.spatialBlend = EnableSpatialAudio ? 1 : 0;
            }
        }
        
        public List<string> GetAvailableMicrophones()
        {
            return Mic.AvailableDevices.Select(m => m.Name).ToList();
        }
        
        public bool TrySetMicrophone(string micName)
        {
            if (_voiceChatSession == null)
                return false;
            
            if (micName != null && TryFindAvailableMicrophoneDevice(micName, out var micDevice))
                return false;
            
            CurrentMicrophoneName = micName;
            RefreshMicrophone();
            return true;
        }
        
        private void RefreshMicrophone()
        {
            if (_voiceChatSession == null)
                return;
            
            if (_voiceChatSession.Input is UniMicInput micInput)
                micInput.Device.StopRecording();

            _voiceChatSession.Input = ResolveAndCreateAudioInput();
        }

        private IAudioInput ResolveAndCreateAudioInput()
        {
            var micDevice = ResolveMicrophone();
            if (micDevice != null)
            {
                Debug.Log($"[AudioManager] Using microphone: {micDevice.Name}");
                micDevice.StartRecording(_MicFrameDuration);
            }
            else
            {
                Debug.Log("[AudioManager] No microphone will be used.");
            }
            return micDevice != null ? new UniMicInput(micDevice) : new UniVoiceEmptyAudioInput();
        }
        
        private Mic.Device ResolveMicrophone()
        {
            if (!_microphonePermissionGranted || !AllowMic)
                return null;
            
            var availableMics = Mic.AvailableDevices;
            var savedMicName = CurrentMicrophoneName;
            
            if(availableMics.Count == 0)
                return null;
            
            if (TryFindAvailableMicrophoneDevice(savedMicName, out var micFound))
                return micFound;
            
            var mic = Mic.AvailableDevices[0];
            CurrentMicrophoneName = mic.Name;
            return mic;
        }

        public bool TryFindAvailableMicrophoneDevice(string micName, out Mic.Device micDevice)
        {
            micDevice = null;
            if (string.IsNullOrEmpty(micName))
                return false;
            
            var availableMics = Mic.AvailableDevices;
            if (availableMics.Count == 0)
                return false;

            return availableMics.TryGetFirst(m => m.Name.Equals(micName), out micDevice);
        }
    }
}
