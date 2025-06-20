#if UNIVOICE
using System;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Cysharp.Threading.Tasks;
using EDIVE.MirrorNetworking;
using EDIVE.MirrorNetworking.Utils;
using UnityEngine;

namespace EDIVE.VoiceChat
{
    public class UniVoiceVoiceChatManager : AVoiceChatManager
    {
        private const string TAG = "[UniVoiceVoiceChatManager]";
        private ClientSession<int> _session;

        public enum InputFilterType
        {
            None = 0,
            GaussianBlur = 1,
            RNNoise = 2,
        }

        public bool EnableSpatialAudio
        {
            get => PlayerPrefs.GetInt("UniVoice_EnableSpatialAudio", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("UniVoice_EnableSpatialAudio", value ? 1 : 0);
                RefreshSpatialAudio();
            }
        }

        public InputFilterType InputFilter
        {
            get => (InputFilterType) PlayerPrefs.GetInt("UniVoice_InputFilter", 1);
            set
            {
                PlayerPrefs.SetInt("UniVoice_InputFilter", (int) value);
                RefreshFilters();
            }
        }

        public int MicFrameDurationMS
        {
            get => PlayerPrefs.GetInt("UniVoice_FrameDuration", 60);
            set
            {
                PlayerPrefs.SetInt("UniVoice_FrameDuration", value);
                ReloadMic();
            }
        }

        public bool AllowMic
        {
            get => PlayerPrefs.GetInt("UniVoice_AllowMic", 1) > 0;
            set
            {
                PlayerPrefs.SetInt("UniVoice_AllowMic", value ? 1 : 0);
                ReloadMic();
            }
        }

        public int CurrentMicIndex
        {
            get => PlayerPrefs.GetInt("UniVoice_CurrentMicIndex", 0);
            set
            {
                if (value == CurrentMicIndex)
                    return;

                value = Mathf.Clamp(value, 0, Mic.AvailableDevices.Count - 1);
                PlayerPrefs.SetInt("UniVoice_CurrentMicIndex", value);
                ReloadMic();
            }
        }

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            var voicePermissionGranted = await VoiceChatUtils.AwaitVoicePermission();
            await UniTask.Yield();

            if (voicePermissionGranted)
            {
                InitializeSession();
            }
        }

        protected override void RegisterService()
        {
            base.RegisterService();
            RegisterService<UniVoiceVoiceChatManager>();
        }

        protected override void UnregisterService()
        {
            base.UnregisterService();
            UnregisterService<UniVoiceVoiceChatManager>();
        }

        private void InitializeSession()
        {
            switch (NetworkUtils.RuntimeMode)
            {
                case NetworkRuntimeMode.Client:
                    InitializeClient();
                    break;

                case NetworkRuntimeMode.Server:
                    InitializeServer();
                    break;

                case NetworkRuntimeMode.Host:
                    InitializeServer();
                    InitializeClient();
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void InitializeClient()
        {
            // Create a client for this device
            var client = new MirrorClient();
            Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorClient object");
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
            IAudioOutputFactory outputFactory = new StreamedAudioSourceOutput.Factory();
            Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

            // With the client, input and output factory ready, we create create the client session
            _session = new ClientSession<int>(client, input, outputFactory);
            Debug.unityLogger.Log(LogType.Log, TAG, "Created session");

            // We add some filters to the input audio
            // - The first is audio blur, so that the audio that's been captured by this client
            // has lesser noise

            //_session.InputFilters.Add(new GaussianAudioBlur());
            _session.InputFilters.Add(new RNNoiseFilter()); // Note: To be made available in Univoice later
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered GaussianAudioBlur as an input filter");

            // - The next one is the Opus encoder filter. This is VERY important. Without this the
            // outgoing data would be very large, usually by a factor of 10 or more.
            _session.InputFilters.Add(new ConcentusEncodeFilter());
            Debug.unityLogger.Log(LogType.Log, TAG, "Registered ConcentusEncodeFilter as an input filter");

            // Next, for incoming audio we register the Concentus decode filter as the audio we'd
            // receive from other clients would be encoded and not readily playable
            _session.OutputFilters.Add(new ConcentusDecodeFilter());
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
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(2));
                    Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} joined");

                    // Make incoming audio from this peer positional
                    // Cast the output so that we can access the AudioSource that's playing this newly joined peers audio
                    var output = _session.PeerOutputs[id] as StreamedAudioSourceOutput;
                    output.gameObject.name = $"Streamed Audio Output ({id})";
                    var audioSource = output.Stream.UnityAudioSource;

                    // Question for Oliver:
                    // Do we have a method like this in the project that allow us to get the avatar gameobject of a client
                    // using the connection ID?
                    var peerAvatar = GetAvatarFromConnId(id);
                    if (peerAvatar != null)
                    {
                        audioSource.transform.SetParent(peerAvatar.PeerRoot); // parent the audiosource to the avatar
                        audioSource.transform.localPosition = Vector3.zero; // set the position to the avatar root

                        audioSource.spatialBlend = EnableSpatialAudio ? 1 : 0; // We set a spatial blend of 1 so that the audio is positional
                        audioSource.maxDistance = 25; // Let the audio of this peer travel to upto 25 meters
                        Debug.unityLogger.Log(LogType.Log, TAG, "Parented audio to avatar gameobject for peer " + id);
                    }
                    else
                    {
                        Debug.unityLogger.Log(LogType.Log, TAG, "Could not find avatar gameobject for peer " + id);
                    }
                });
            };

            // When a peer leaves, destroy the UI representing them
            client.OnPeerLeft += id => { Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} left"); };
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

        private void RefreshFilters()
        {
            // This method is called when the user changes the filters in the settings
            // We can use this to refresh the filters in the session
            if (_session == null)
                return;

            _session.InputFilters.Clear();
            _session.OutputFilters.Clear();

            switch (InputFilter)
            {
                case InputFilterType.None:
                    break;
                case InputFilterType.GaussianBlur:
                    _session.InputFilters.Add(new GaussianAudioBlur());
                    break;
                case InputFilterType.RNNoise:
                    _session.InputFilters.Add(new RNNoiseFilter());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // The next one is the Opus encoder filter. This is VERY important. Without this the
            // outgoing data would be very large, usually by a factor of 10 or more.
            _session.InputFilters.Add(new ConcentusEncodeFilter());

            // Next, for incoming audio we register the Concentus decode filter as the audio we'd
            // receive from other clients would be encoded and not readily playable
            _session.OutputFilters.Add(new ConcentusDecodeFilter());
        }

        private void InitializeServer()
        {
            // We create a server. If this code runs in server mode, MirrorServer will take care
            // or automatically handling all incoming messages.
            var server = new MirrorServer();
            Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorServer object");

            // Subscribe to some server events
            server.OnServerStart += () => {
                Debug.unityLogger.Log(LogType.Log, TAG, "Server started");
            };

            server.OnServerStop += () => {
                Debug.unityLogger.Log(LogType.Log, TAG, "Server stopped");
            };
        }

        // Todo probably ask PlayerManager for the avatar when its moved to the sumbodule
        private VoiceChatPlayerController GetAvatarFromConnId(int id)
        {
            foreach (var avatar in FindObjectsByType<VoiceChatPlayerController>(FindObjectsSortMode.None))
            {
                var netId = avatar.ConnectionID;

                if (netId == id)
                    return avatar;
            }

            return null;
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
