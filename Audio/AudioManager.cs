// Author: František Holubec
// Created: 02.02.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Adrenak.BRW;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Networks;
using Cysharp.Threading.Tasks;
using EDIVE.Audio.Playback;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.NativeUtils;
using EDIVE.Networking.Players;
using EDIVE.OdinExtensions.Attributes;
using PurrNet;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace EDIVE.Audio
{
    public class AudioManager : ALoadableServiceBehaviour<AudioManager>
    {
        [SerializeField]
        [OnValueChanged(nameof(OnPresetChanged))]
        [Tooltip("Picking a preset overwrites the fields below. Switch to Custom to keep your manual tweaks.")]
        private VoiceChatPreset _Preset = VoiceChatPreset.FullBand48KHzStandard;

        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private VoiceChatConfig _Config = new();

        [SerializeField]
        [SuffixLabel("m", true)]
        private float _SpatialAudioDistance = 25f;

        [SerializeField]
        [Required]
        private AudioMixerGroup _VoiceMixerGroup;

        [SerializeField]
        [Required]
        private AudioMixer _Mixer;

        [SerializeField]
        [EnhancedValueDropdown(nameof(GetExposedParameterOptions), IsUniqueList = true)]
        private List<string> _ManagedVolumeParameters = new() { "MasterVolume", "MusicVolume", "SFXVolume", "VoiceVolume" };

        private const float MIN_VOLUME_LINEAR = 0.0001f;
        // +20 dB is the mixer's Attenuation ceiling (linear 10). Lets a group boost above unity (1 = 0 dB).
        private const float MAX_VOLUME_LINEAR = 10f;

        private ClientSession<PlayerID> _voiceChatSession;
        private bool _microphonePermissionGranted = false;

        private readonly List<object> _voiceChatMuteRequests = new();
        public bool VoiceChatMuted => _voiceChatMuteRequests.Any();

        private PurrNetClient _uniVoiceClient;
        private PurrNetServer _uniVoiceServer;
        private IAudioInput _currentAudioInput;

        // Unprocessed local audio frames, triggered when a new frame is captured from the microphone
        public event Action<AudioFrame> LocalRawAudioFrameReady;

        // Triggered when any audio frame is received (local and remote), frames are encoded
        public event Action<PlayerID, AudioFrame> UserAudioFrameReady;

        private List<IAudioFilter> _encodeFilters;
        private CapturingAudioFilter _capturingFilter;
        
        [DisableInEditorMode]
        [ShowInInspector]
        public bool EnableSpatialAudio
        {
            get => PlayerPrefs.GetInt("Audio_SpatialAudio", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("Audio_SpatialAudio", value ? 1 : 0);
                RefreshSpatialAudio();
            }
        }

        [DisableInEditorMode]
        [ShowInInspector]
        public bool AllowMic
        {
            get => PlayerPrefs.GetInt("Audio_AllowMic", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("Audio_AllowMic", value ? 1 : 0);
                RefreshAudioInput();
            }
        }

        [DisableInEditorMode]
        [ShowInInspector]
        public bool EnableVoiceChat
        {
            get => PlayerPrefs.GetInt("Audio_EnableVoiceChat", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("Audio_EnableVoiceChat", value ? 1 : 0);
                RefreshVoiceChatState();
            }
        }
        
        public string CurrentMicrophoneName
        {
            get => PlayerPrefs.GetString("Audio_MicName", string.Empty);
            private set => PlayerPrefs.SetString("Audio_MicName", value);
        }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            ApplySavedVolumes();
            InitializeAudioInput();
            InitializeVoiceChat();
            return UniTask.CompletedTask;
        }

        private static string VolumePrefKey(string parameter) => $"Audio_Volume_{parameter}";
        
        public float GetVolume(string parameter) => PlayerPrefs.GetFloat(VolumePrefKey(parameter), 1f);
        
        public void SetVolume(string parameter, float linear)
        {
            linear = Mathf.Clamp(linear, 0f, MAX_VOLUME_LINEAR);
            PlayerPrefs.SetFloat(VolumePrefKey(parameter), linear);
            ApplyVolume(parameter, linear);
        }

        private void ApplyVolume(string parameter, float linear)
        {
            if (_Mixer == null || string.IsNullOrEmpty(parameter))
                return;
            // Mixer volumes are dB (logarithmic); convert from the linear slider value.
            var db = Mathf.Log10(Mathf.Max(linear, MIN_VOLUME_LINEAR)) * 20f;
            _Mixer.SetFloat(parameter, db);
        }

        private void ApplySavedVolumes()
        {
            foreach (var parameter in _ManagedVolumeParameters)
                ApplyVolume(parameter, GetVolume(parameter));
        }
        
        private void InitializeAudioInput()
        {
            // No microphone needed in headless mode
            if (PlatformUtils.IsHeadless()) 
                return;
            
            Mic.Init();
            RefreshAudioInput();
                
            AudioUtils.CheckMicrophonePermission(micPermissionGranted =>
            {
                _microphonePermissionGranted = micPermissionGranted;
                if (micPermissionGranted)
                {
                    RefreshAudioInput();
                }
            });
        }

        private void InitializeVoiceChat()
        {
            if (!PlatformUtils.IsHeadless()) 
                InitializeClient();
            InitializeServer();
        }
      
        private void InitializeClient()
        {
            _uniVoiceClient = new PurrNetClient();
            _voiceChatSession = new ClientSession<PlayerID>(_uniVoiceClient, _currentAudioInput, () =>
            {
                var audioOutput = StreamedAudioSourceOutput.New();
                ApplyPlaybackSettings(audioOutput);
                return audioOutput;
            });

            _capturingFilter = new CapturingAudioFilter();
            _capturingFilter.AudioFrameCaptured += OnVoiceChatFrameCaptured;

            _encodeFilters = BuildEncodeFilters();
            _voiceChatSession.InputFilters.AddRange(_encodeFilters);
            _voiceChatSession.AddOutputFilter<ConcentusDecodeFilter>(() => new ConcentusDecodeFilter()); // Opus decoding
            
            _uniVoiceClient.OnJoined += OnVoiceChatClientJoined;
            _uniVoiceClient.OnLeft += OnVoiceChatClientLeft;
            _uniVoiceClient.OnPeerJoined += OnVoiceChatPeerJoined;
            _uniVoiceClient.OnPeerLeft += OnVoiceChatPeerLeft;

            NetworkManager.main.Subscribe<PurrNetBroadcast>(OnClientReceivedAudioMessage, asServer: false);

            RefreshVoiceChatState();
        }
        
        private void OnClientReceivedAudioMessage(PlayerID sender, PurrNetBroadcast message, bool asServer)
        {
            var reader = new BytesReader(message.data);
            var messageTag = reader.ReadString();

            if (!messageTag.Equals(AudioBroadcastTags.AUDIO_FRAME))
                return;

            var senderId = reader.ReadPlayerID();
            var frame = new AudioFrame
            {
                timestamp = reader.ReadLong(),
                frequency = reader.ReadInt(),
                channelCount = reader.ReadInt(),
                samples = reader.ReadByteArray()
            };

            // On host we skip here so the server path doesn't double-invoke UserAudioFrameReady.
            if (NetworkManager.main == null || !NetworkManager.main.isServer)
            {
                frame.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                UserAudioFrameReady?.Invoke(senderId, frame);
            }
        }

        private void OnVoiceChatClientLeft()
        {
            Debug.Log("[AudioManager] You left the chatroom");
        }
        
        private void OnVoiceChatClientJoined(PlayerID playerID, List<PlayerID> playerIds)
        {
            Debug.Log($"[AudioManager] You are Peer ID {playerID} your peers are {string.Join(", ", playerIds)}");
        }

        private void OnVoiceChatPeerLeft(PlayerID playerID)
        {
            Debug.Log($"[AudioManager] Peer '{playerID}' left the chatroom");
        }

        private void OnVoiceChatPeerJoined(PlayerID playerID)
        {
            Debug.Log($"[AudioManager] Peer '{playerID}' joined the chatroom");

            var output = _voiceChatSession.PeerOutputs[playerID] as StreamedAudioSourceOutput;
            if (output == null)
            {
                Debug.LogError($"[AudioManager] Could not get StreamedAudioSourceOutput for peer {playerID}");
                return;
            }
            output.gameObject.name = $"StreamedAudioOutput ({playerID})";
            InitializePeerSpatialAudioAsync(playerID, output).Forget();
        }
        
        private async UniTask InitializePeerSpatialAudioAsync(PlayerID playerID, StreamedAudioSourceOutput output)
        {
            var playerManager = AppCore.Services.Get<NetworkPlayerManager>();
            var playerController = await playerManager.AwaitPlayerController(playerID);

            if (playerController == null)
            {
                Debug.LogWarning($"[AudioManager] Could not find player controller for peer {playerID}");
                return;
            }

            if (!playerController.TryGetComponent<VoiceChatPlayerController>(out var peerAvatar))
            {
                Debug.LogWarning($"[AudioManager] Player controller for peer {playerID} does not have a VoiceChatPlayerController component");
                return;
            }
            
            var audioSource = output.Stream.UnityAudioSource;
            audioSource.spatialBlend = EnableSpatialAudio ? 1 : 0; 
            audioSource.maxDistance = _SpatialAudioDistance;
            
            peerAvatar.InitializePeerAudioOutput(output);
            
            Debug.Log($"[AudioManager] AudioSource of player '{playerID}' ");
        }

        private void InitializeServer()
        {
            _uniVoiceServer = new PurrNetServer();
            _uniVoiceServer.OnServerStart += OnVoiceChatServerStarted;
            _uniVoiceServer.OnServerStop += OnVoiceChatServerStopped;

            NetworkManager.main.Subscribe<PurrNetBroadcast>(OnServerReceivedAudioMessage, asServer: true);
        }

        private void OnServerReceivedAudioMessage(PlayerID sender, PurrNetBroadcast message, bool asServer)
        {
            var reader = new BytesReader(message.data);
            var messageTag = reader.ReadString();

            if (!messageTag.Equals(AudioBroadcastTags.AUDIO_FRAME))
                return;

            var senderId = reader.ReadPlayerID();
            var frame = new AudioFrame
            {
                timestamp = reader.ReadLong(),
                frequency = reader.ReadInt(),
                channelCount = reader.ReadInt(),
                samples = reader.ReadByteArray()
            };

            frame.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            UserAudioFrameReady?.Invoke(senderId, frame);
        }

        private void OnVoiceChatServerStarted()
        {
            Debug.Log("[AudioManager] Voice chat server started");
        }

        private void OnVoiceChatServerStopped()
        {
            Debug.Log("[AudioManager] Voice chat server stopped");
        }
        
        public bool HasVoiceChatMuteRequest(object requester)
        {
            return _voiceChatMuteRequests.Contains(requester);
        }
        
        public void AddVoiceChatMuteRequest(object requester)
        {
            if (!HasVoiceChatMuteRequest(requester)) 
                _voiceChatMuteRequests.Add(requester);
            RefreshVoiceChatState();
        }

        public void RemoveVoiceChatMuteRequest(object requester)
        {
            _voiceChatMuteRequests.Remove(requester);
            RefreshVoiceChatState();
        }

        private void RefreshVoiceChatState()
        {
            if (_voiceChatSession == null)
                return;
            _voiceChatSession.InputEnabled = EnableVoiceChat && !VoiceChatMuted;
            _voiceChatSession.OutputsEnabled = EnableVoiceChat;
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
        
        private void ApplyPlaybackSettings(StreamedAudioSourceOutput output)
        {
            if (output == null || output.Stream == null)
                return;
            output.Stream.UnityAudioSource.outputAudioMixerGroup = _VoiceMixerGroup;
            output.Stream.TargetLatency = _Config.TargetLatencySeconds;
            output.Stream.PitchMaxCorrection = _Config.PitchMaxCorrection;
            output.Stream.PitchProportionalGain = _Config.PitchProportionalGain;
            output.Stream.DownwardPitchCorrectionScale = _Config.DownwardPitchCorrectionScale;
        }

        private List<IAudioFilter> BuildEncodeFilters()
        {
            var filters = new List<IAudioFilter>();
            if (_Config.UseRnNoise)
                filters.Add(new RNNoiseFilter()); // Noise suppression
            if (_Config.UseSimpleVad)
                filters.Add(new SimpleVadFilter(new SimpleVad(_Config.BuildVadConfig()))); // Voice activity detection
            filters.Add(new ConcentusEncodeFilter(
                _Config.OpusFrequency,
                resamplerQuality: _Config.ResamplerQuality,
                encoderComplexity: _Config.EncoderComplexity,
                encoderBitrate: _Config.EncoderBitrate)); // Opus encoding
            filters.Add(_capturingFilter);
            return filters;
        }

        private void OnPresetChanged()
        {
            if (_Preset != VoiceChatPreset.Custom) 
                _Config.ApplyPreset(_Preset);
        }
        
        [Button, GUIColor(0.6f, 1f, 0.6f)]
        [DisableInEditorMode]
        [PropertyTooltip("Rebuilds the Opus encoder + filter chain and restarts the microphone with the current settings.")]
        private void ApplyVoiceChatConfig()
        {
            if (!Application.isPlaying)
                return;

            RefreshAudioInput();

            if (_voiceChatSession == null || _encodeFilters == null)
                return;

            _voiceChatSession.InputFilters.Clear();
            _encodeFilters = BuildEncodeFilters();
            _voiceChatSession.InputFilters.AddRange(_encodeFilters);

            foreach (var peerOutput in _voiceChatSession.PeerOutputs.Values)
            {
                if (peerOutput is StreamedAudioSourceOutput streamedOutput)
                    ApplyPlaybackSettings(streamedOutput);
            }

            Debug.Log($"[AudioManager] Voice chat config applied: mic {_Config.MicSamplingFrequency} Hz @ {_Config.MicFrameDurationMs} ms, Opus {(int)_Config.OpusFrequency} Hz, complexity {_Config.EncoderComplexity}, bitrate {_Config.EncoderBitrate} bps, target latency {_Config.TargetLatencySeconds * 1000f:F0} ms.");
        }

        public IEnumerable<string> GetAvailableMicrophones()
        {
            if (!Application.isPlaying) return Enumerable.Empty<string>();
            return Mic.AvailableDevices.Select(m => m.Name);
        }
        
        public bool TrySetMicrophone(string micName)
        {
            if (_voiceChatSession == null)
                return false;
            
            if (micName == null || !TryFindAvailableMicrophoneDevice(micName, out _))
                return false;
            
            CurrentMicrophoneName = micName;
            RefreshAudioInput();
            return true;
        }

        private void RefreshAudioInput()
        {
            var micDevice = ResolveMicrophone();
            if (_currentAudioInput is UniMicInput uniMicInput)
            {
                if (uniMicInput.Device == micDevice)
                    return;
                uniMicInput.Device.StopRecording();
            }

            if (micDevice != null)
            {
                Debug.Log($"[AudioManager] Using microphone: {micDevice.Name}");
                micDevice.StartRecording(ResolveMicrophoneFrequency(micDevice), _Config.MicFrameDurationMs);
            }
            else
            {
                Debug.Log("[AudioManager] No microphone will be used.");
            }

            if (_currentAudioInput != null)
            {
                _currentAudioInput.Dispose();
                _currentAudioInput.OnFrameReady -= OnRawAudioFrameReady;
            }
            
            _currentAudioInput = micDevice != null ? new UniMicInput(micDevice) : new EmptyAudioInput();
            _currentAudioInput.OnFrameReady += OnRawAudioFrameReady;
            if (_voiceChatSession != null)
                _voiceChatSession.Input = _currentAudioInput;
        }
        
        private int ResolveMicrophoneFrequency(Mic.Device micDevice)
        {
            var desired = _Config.MicSamplingFrequency;
            if (micDevice.SupportsAnyFrequency)
                return desired;

            if (micDevice.MaxFrequency == desired || micDevice.MinFrequency == desired)
                return desired;

            // If the desired frequency is not supported, use the maximum supported frequency
            return micDevice.MaxFrequency;
        }
        
        private void OnVoiceChatFrameCaptured(AudioFrame frame)
        {
            // Only run if voice chat is enabled, (we reuse the filters, that's why)
            if (!_voiceChatSession.InputEnabled) 
                return;
            
            if (UserAudioFrameReady == null) 
                return;

            var manager = NetworkManager.main;
            if (manager == null || manager.isServer) 
                return;
            
            frame.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var localId = manager.localPlayer;
            UserAudioFrameReady.Invoke(localId, frame);
        }

        private void OnRawAudioFrameReady(AudioFrame frame)
        {
            frame.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LocalRawAudioFrameReady?.Invoke(frame);

            // if voice chat is enabled we will get frames from OnVoiceChatFrameCaptured
            if (_voiceChatSession == null || _voiceChatSession.InputEnabled)
                return;
            
            if (UserAudioFrameReady == null)
                return;

            var manager = NetworkManager.main;
            if (manager == null || manager.isServer) 
                return;

            // Process the raw audio frame through the same filters as the voice chat frames
            if (!AudioUtils.TryProcessAudioFrame(ref frame, _encodeFilters)) 
                return;
            
            frame.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var localId = manager.localPlayer;
            UserAudioFrameReady.Invoke(localId, frame);
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

#if UNITY_EDITOR
        [DisableInEditorMode]
        [ShowInInspector]
        [ValueDropdown("GetAvailableMicrophones")]
        [LabelText("Current Microphone")]
        private string CurrentMicrophoneDropdown
        {
            get => CurrentMicrophoneName;
            set => TrySetMicrophone(value);
        }
        
        private IEnumerable<string> GetExposedParameterOptions() => _Mixer.GetExposedParameterOptions();
#endif
    }
}
