using System;
using Adrenak.UniMic;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using Mirror;
using UnityEngine;

namespace EDIVE.VoiceChat
{
    public class UniVoiceVoiceChatManager : ALoadableServiceBehaviour<UniVoiceVoiceChatManager>
    {
        private const string TAG = "[UniVoiceVoiceChatManager]";
        private ClientSession<int> _session;

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            var voicePermissionGranted = await VoiceChatUtils.AwaitVoicePermission();
            await UniTask.Yield();

            if (voicePermissionGranted)
            {
                InitializeSession();
            }
        }

        private void InitializeSession()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Initializing UniVoice");

#if UNITY_SERVER
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

        return;
#endif
            // From here on, handle running on a client

            // Create a client for this device
            var client = new MirrorClient();
            Debug.unityLogger.Log(LogType.Log, TAG, "Created MirrorClient object");
            IAudioInput input;
            IAudioOutputFactory outputFactory;

            // Question for Oliver:
            // Are we planning to run on devices that ALWAYS have a recording device? This would be true
            // for Android devices (VR and non VR) but may not be true on desktop.

            // Since in this sample we use microphone input via UniMic, we first check if there
            // are any mic devices available.
            Mic.Init(); // Must do this to use the Mic class
            if (Mic.AvailableDevices.Count == 0)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Device has no microphones." +
                                                        "Will only be able to hear other clients, cannot send any audio.");
                input = new Edive_UnivoiceEmptyAudioInput();
            }
            else
            {
                // Get the first recording device that we have available and start it.
                // Then we create a UniMicInput instance that requires the mic object
                // For more info on UniMic refer to https://www.github.com/adrenak/unimic
                var mic = Mic.AvailableDevices[0];
                mic.StartRecording(16000, 50);
                Debug.unityLogger.Log(LogType.Log, TAG, "Started recording with Mic device named." +
                                                        mic.Name + $" at frequency {mic.SamplingFrequency} with frame duration {mic.FrameDurationMS} ms.");
                input = new UniMicInput(mic);
                Debug.unityLogger.Log(LogType.Log, TAG, "Created UniMicInput");
            }

            // We want the incoming audio from peers to be played via the StreamedAudioSourceOutput
            // implementation of IAudioSource interface. So we get the factory for it.
            outputFactory = new StreamedAudioSourceOutput.Factory();
            Debug.unityLogger.Log(LogType.Log, TAG, "Using StreamedAudioSourceOutput.Factory as output factory");

            // With the client, input and output factory ready, we create create the client session
            _session = new ClientSession<int>(client, input, outputFactory);
            Debug.unityLogger.Log(LogType.Log, TAG, "Created session");

            // We add some filters to the input audio
            // - The first is audio blur, so that the audio that's been captured by this client
            // has lesser noise
            // TODO: Replace this with RNNoise for better noise removal
            _session.InputFilters.Add(new GaussianAudioBlur()); // Note: Once using RNNoiseFilter, this is obsolete
            // session.InputFilters.Add(new RNNoiseFilter()); // Note: To be made available in Univoice later
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
            client.OnJoined += (id, peerIds) => { Debug.unityLogger.Log(LogType.Log, TAG, $"You are Peer ID {id} your peers are {string.Join(", ", peerIds)}"); };

            client.OnLeft += () => { Debug.unityLogger.Log(LogType.Log, TAG, "You left the chatroom"); };

            // When a peer joins, we instantiate a new peer view
            client.OnPeerJoined += id =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} joined");

                // Make incoming audio from this peer positional
                // Cast the output so that we can access the AudioSource that's playing this newly joined peers audio
                var output = _session.PeerOutputs[id] as StreamedAudioSourceOutput;
                AudioSource audioSource = output.Stream.UnityAudioSource;

                // Question for Oliver:
                // Do we have a method like this in the project that allow us to get the avatar gameobject of a client
                // using the connection ID?
                var peerAvatar = GetAvatarFromConnId(id);
                if (peerAvatar != null)
                {
                    audioSource.transform.SetParent(peerAvatar.PeerRoot); // parent the audiosource to the avatar
                    audioSource.transform.localPosition = Vector3.zero; // set the position to the avatar root

                    audioSource.spatialBlend = 1; // We set a spatial blend of 1 so that the audio is positional
                    audioSource.maxDistance = 25; // Let the audio of this peer travel to upto 25 meters
                    Debug.unityLogger.Log(LogType.Log, TAG, "Parented audio to avatar gameobject for peer " + id);
                }
                else
                {
                    Debug.unityLogger.Log(LogType.Log, TAG, "Could not find avatar gameobject for peer " + id);
                }
            };

            // When a peer leaves, destroy the UI representing them
            client.OnPeerLeft += id => { Debug.unityLogger.Log(LogType.Log, TAG, $"Peer {id} left"); };
        }

        // Todo probably ask PlayerManager for the avatar when its moved to the sumbodule
        private VoiceChatAvatar GetAvatarFromConnId(int id)
        {
            foreach (var avatar in FindObjectsOfType<VoiceChatAvatar>())
            {
                var netId = avatar.NetworkIdentity.netId;
                var myNetId = NetworkClient.connection.identity.netId;
                if (netId == myNetId || netId == 0)
                    continue;

                if (netId == id)
                    return avatar;
            }

            return null;
        }
    }
}
