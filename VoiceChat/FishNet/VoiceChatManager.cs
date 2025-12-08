#if UNIVOICE && FISHNET
using System;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.Players;
using FishNet;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.VoiceChat
{
    public class VoiceChatManager : AVoiceChatManager
    {
        private const string TAG = "[VoiceChatManager]";
        private ClientSession<int> _session;

        private NetworkManager _networkManager;
        
        public override bool EnableSpatialAudio
        {
            get => PlayerPrefs.GetInt("VoiceChat_EnableSpatialAudio", 1) > 0;
            set
            {
                if (EnableSpatialAudio == value)
                    return;
                PlayerPrefs.SetInt("VoiceChat_EnableSpatialAudio", value ? 1 : 0);
                RefreshSpatialAudio();
            }
        }
        
        public override int MicFrameDurationMS
        {
            get => PlayerPrefs.GetInt("VoiceChat_FrameDuration", 40);
            set
            {
                if (MicFrameDurationMS == value)
                    return;
                PlayerPrefs.SetInt("VoiceChat_FrameDuration", value);
                ReloadMic();
            }
        }

        public override bool AllowMic
        {
            get => PlayerPrefs.GetInt("VoiceChat_AllowMic", 1) > 0;
            set
            {
                if (AllowMic == value)
                    return;
                PlayerPrefs.SetInt("VoiceChat_AllowMic", value ? 1 : 0);
                ReloadMic();
            }
        }

        public override int CurrentMicIndex
        {
            get => PlayerPrefs.GetInt("VoiceChat_CurrentMicIndex", 0);
            set
            {
                if (value == CurrentMicIndex)
                    return;
                value = Mathf.Clamp(value, 0, Mic.AvailableDevices.Count - 1);
                PlayerPrefs.SetInt("VoiceChat_CurrentMicIndex", value);
                ReloadMic();
            }
        }

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = InstanceFinder.NetworkManager;
            
            var voicePermissionGranted = await VoiceChatUtils.AwaitVoicePermission();
            await UniTask.Yield();

            if (voicePermissionGranted)
            {
                try
                {
                    InitializeSession();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        protected override void RegisterService()
        {
            base.RegisterService();
            RegisterService<VoiceChatManager>();
        }

        protected override void UnregisterService()
        {
            base.UnregisterService();
            UnregisterService<VoiceChatManager>();
        }

        public static bool IsHeadless() =>
#if UNITY_SERVER && !UNITY_EDITOR
            true;
#else
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
#endif
        
        private void InitializeSession()
        {
            if (!IsHeadless())
            {
                InitializeClient();
            }
            InitializeServer();
        }

        private void InitializeClient()
        {
            // Create a client for this device
            var client = new FishNetClient();
            Debug.unityLogger.Log(LogType.Log, TAG, "Created Client object");
            IAudioInput input;

            // Question for Oliver:
            // Are we planning to run on devices that ALWAYS have a recording device? This would be true
            // for Android devices (VR and non VR) but may not be true on desktop.

            // Since in this sample we use microphone input via UniMic, we first check if there
            // are any mic devices available.
            Mic.Init(); // Must do this to use the Mic class
            if (Mic.AvailableDevices.Count == 0)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Device has no microphones");
                input = new UniVoiceEmptyAudioInput();
            }
            else if (!AllowMic)
            {
                input = new UniVoiceEmptyAudioInput();
            }
            else
            {
                // Get the first recording device that we have available and start it.
                // Then we create a UniMicInput instance that requires the mic object
                // For more info on UniMic refer to https://www.github.com/adrenak/unimic
                CurrentMicIndex = Mathf.Clamp(CurrentMicIndex, 0, Mic.AvailableDevices.Count - 1);
                var mic = Mic.AvailableDevices[CurrentMicIndex];
                mic.StartRecording(MicFrameDurationMS);
                Debug.unityLogger.Log(LogType.Log, TAG, $"Started recording with Mic device named.{mic.Name} at frequency {mic.SamplingFrequency} with frame duration {mic.FrameDurationMS} ms.");
                input = new UniMicInput(mic);
                Debug.unityLogger.Log(LogType.Log, TAG, "Created UniMicInput");
            }

            // We want the incoming audio from peers to be played via the StreamedAudioSourceOutput
            // implementation of IAudioSource interface. So we get the factory for it.
            Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

            // With the client, input and output factory ready, we create create the client session
            _session = new ClientSession<int>(client, input, () =>
            {
                var audioOutput = StreamedAudioSourceOutput.New();
                audioOutput.Stream.TargetLatency = 0.3f;
                audioOutput.Stream.PitchMaxCorrection = 0.05f;
                audioOutput.Stream.PitchProportionalGain = 0.2f;
                audioOutput.Stream.DownwardPitchCorrectionScale = 1f;
                return audioOutput;
            });
            Debug.unityLogger.Log(LogType.Log, TAG, "Created session");
            
            _session.InputFilters.Add(new RNNoiseFilter());
            _session.InputFilters.Add(new SimpleVadFilter(new SimpleVad()));
            _session.InputFilters.Add(new ConcentusEncodeFilter());
            
            _session.AddOutputFilter<ConcentusDecodeFilter>(() => new ConcentusDecodeFilter());
            
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered ConcentusDecodeFilter as an output filter");

            // We subscribe to some client events to show updates on the UI when you join or leave
            client.OnJoined += (id, peerIds) =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"You are Peer ID {id} your peers are {string.Join(", ", peerIds)}");
            };

            client.OnLeft += () =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "You left the chatroom");
            };

            // When a peer joins, we instantiate a new peer view
            client.OnPeerJoined += id =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} joined");

                var output = _session.PeerOutputs[id] as StreamedAudioSourceOutput;
                output.gameObject.name = $"StreamedAudioOutput ({id})";

                // Parent the audio source to the player controller
                UniTask.Void(async () =>
                {
                    var playerManager = AppCore.Services.Get<NetworkPlayerManager>();
                    var playerController = await playerManager.AwaitPlayerController(id);

                    if (playerController == null)
                    {
                        Debug.unityLogger.Log(LogType.Error, TAG, $"Could not find player controller for peer {id}");
                        return;
                    }

                    if (playerController.TryGetComponent<VoiceChatPlayerController>(out var peerAvatar))
                    {
                        if (peerAvatar != null)
                        {
                            var audioSource = output.Stream.UnityAudioSource;
                            audioSource.transform.SetParent(peerAvatar.PeerRoot); // parent the audiosource to the avatar
                            audioSource.transform.localPosition = Vector3.zero; // set the position to the avatar root

                            audioSource.spatialBlend = EnableSpatialAudio ? 1 : 0; // We set a spatial blend of 1 so that the audio is positional
                            audioSource.maxDistance = 25; // Let the audio of this peer travel to upto 25 meters
                            Debug.unityLogger.Log(LogType.Log, TAG, $"Parented audio to player controller for peer {id}");
                        }
                        else
                        {
                            Debug.unityLogger.Log(LogType.Log, TAG, $"Player controller for peer {id} does not have a VoiceChatPlayerController component");
                        }
                    }
                });
            };

            // When a peer leaves, destroy the UI representing them
            client.OnPeerLeft += id =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} left");
            };
        }

        public void RefreshSpatialAudio()
        {
            if (_session == null)
                return;

            foreach (var output in _session.PeerOutputs.Values)
            {
                if (output is not StreamedAudioSourceOutput streamedOutput)
                    continue;

                streamedOutput.Stream.UnityAudioSource.spatialBlend = EnableSpatialAudio ? 1 : 0;
            }
        }
        
        private void InitializeServer()
        {
            // We create a server. If this code runs in server mode, MirrorServer will take care
            // or automatically handling all incoming messages.
            var server = new FishNetServer();
            Debug.unityLogger.Log(LogType.Log, TAG, "Created Server object");

            // Subscribe to some server events
            server.OnServerStart += () => {
                Debug.unityLogger.Log(LogType.Log, TAG, "Server started");
            };

            server.OnServerStop += () => {
                Debug.unityLogger.Log(LogType.Log, TAG, "Server stopped");
            };
        }

        private void ReloadMic()
        {
            if (_session == null)
                return;

            if (_session.Input is UniMicInput micInput)
                micInput.Device.StopRecording();

            if (Mic.AvailableDevices.Count == 0 || !AllowMic)
            {
                _session.Input = new UniVoiceEmptyAudioInput();
                return;
            }

            var mic = Mic.AvailableDevices[CurrentMicIndex];
            mic.StartRecording(MicFrameDurationMS);
            Debug.unityLogger.Log(LogType.Log, TAG, $"Started recording with Mic device named.{mic.Name} at frequency {mic.SamplingFrequency} with frame duration {mic.FrameDurationMS} ms.");
            _session.Input = new UniMicInput(mic);
        }
    }
}
#endif
