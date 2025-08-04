using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.VoiceRecording;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Time.TimeSpanUtils;
using EDIVE.VoiceChat;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VoiceRecording
{
    // Class responsible for creating voice recordings as a form of notes.
    // When the recording process starts, the player is muted.
    // When the process ends, the player is muted/unmuted - the same he was before recording.
    // The recording process is shown on the UI slider
    // Maximum time of one recording is 60 seconds. This can be adjusted by changing the "maxClipDurationSec"
    public class VoiceRecordingManager : ALoadableServiceBehaviour<VoiceRecordingManager>
    {
        private AVoiceChatManager _voiceChatManager;

        [SerializeField]
        private int _Frequency = 44100;

        [TimeSpanDrawerSettings(TimeUnit.Minutes)]
        [SerializeField]
        private UTimeSpan _MaxClipDuration = TimeSpan.FromSeconds(60);

        [Tooltip("How many times should the microphone audio buffer be larger that the reading window")]
        [SerializeField]
        private int _MicrophoneBufferSize = 2;

        private AudioClip _micRecording;

        // If the player was muted before the recording, they will stay muted after
        private bool _mutedPreviously = false;

        private float _recordingStartTime;

        [ShowInInspector]
        public bool Recording { get; private set; }
        public Signal<bool> RecordingStateChanged { get; } = new();

        public TimeSpan MaxClipDuration => _MaxClipDuration;

        [ShowInInspector]
        [KeepRefreshing]
        [TimeSpanDrawerSettings(TimeUnit.Minutes)]
        public TimeSpan CurrentRecordingTime => Recording ? TimeSpan.FromSeconds(UnityEngine.Time.time - _recordingStartTime) : TimeSpan.Zero;

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(AVoiceChatManager));
        }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            Debug.Log("Loading VoiceRecordingManager.");

            _voiceChatManager = AppCore.Services.Get<AVoiceChatManager>();

            // microphone set-up
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone found!");
            }
            else
            {
                // Passing null to Microphone methods selects default device
                Microphone.GetDeviceCaps(null, out var minFreq, out var maxFreq);
                _Frequency = Mathf.Clamp(_Frequency, minFreq, maxFreq);
            }

            AppCore.Services.Register(this);
            Debug.Log("Loaded VoiceRecordingManager.");
            return UniTask.CompletedTask;
        }

        [Button]
        public void ToggleVoiceRecording()
        {
            if (!Recording)
                StartVoiceRecording();
            else
                EndVoiceRecording();
        }

        public void ToggleVoiceRecording(VoiceRecordingController controller)
        {
            if (!Recording)
            {
                StartVoiceRecording();
                controller.IsRecording = true;
            }
            else if (controller.IsRecording)
            {
                EndVoiceRecording();
                controller.IsRecording = false;
            }
        }

        [Button]
        public void StartVoiceRecording()
        {
            Debug.Log("Starting voice recording.");
            Recording = true;
            _recordingStartTime = UnityEngine.Time.time;

            Debug.Log("Starting microphone.");
            _micRecording = Microphone.Start(null, true, (int) MaxClipDuration.TotalSeconds * _MicrophoneBufferSize, _Frequency);

            // Todo End voice recording after max duration
            // Test overriding audio clip when loop = true

            // this creates a dependency on voiceChatManager. Do we want a case when
            // voice chat doesn't exist? Or do we want to make this command go to AppCore?
            _mutedPreviously = _voiceChatManager.IsMicMuted();
            _voiceChatManager.SetMicMuted(Recording);
            RecordingStateChanged.Dispatch(Recording);
            Debug.Log("Voice recording started.");
        }

        [Button]
        public void EndVoiceRecording()
        {
            Debug.Log("Ending voice recording.");

            // end recording
            Recording = false;
            Microphone.End(null);
            SaveVoiceRecording(_micRecording);

            // unmute player unless they were muted before the recording
            _voiceChatManager.SetMicMuted(_mutedPreviously);
            RecordingStateChanged.Dispatch(Recording);
            Debug.Log("Voice recording ended.");
        }

        private void SaveVoiceRecording(AudioClip currRecording)
        {
            Debug.Log("Saving voice recording.");

            // Specific name of the recording - makes sure recording names are unique by adding the time they were created
            string name = "/VoiceRecording_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");

            SavWav.Save(name, currRecording);
        }
    }
}