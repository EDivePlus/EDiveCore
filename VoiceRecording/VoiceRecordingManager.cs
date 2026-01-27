using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Audio;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Time.TimeSpanUtils;
using EDIVE.VoiceChat;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VoiceRecording
{
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

        [ShowInInspector]
        [KeepRefreshing]
        [TimeSpanDrawerSettings(TimeUnit.Minutes)]
        public TimeSpan CurrentRecordingTime => Recording ? TimeSpan.FromSeconds(UnityEngine.Time.time - _recordingStartTime) : TimeSpan.Zero;

        public TimeSpan MaxClipDuration => _MaxClipDuration;
        
        [ShowInInspector]
        public bool Recording { get; private set; }
        
        public static string RecordingsFolderPath =>
#if UNITY_EDITOR
            PathUtility.GetAbsolutePath("VoiceRecordings/");
#elif UNITY_STANDALONE
            Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath)!.FullName, "VoiceRecordings");
#else
            Path.Combine(UnityEngine.Application.persistentDataPath, "VoiceRecordings");
#endif
        
        public Signal<bool> RecordingStateChanged { get; } = new();

        private AudioClip _micRecording;
        private float _recordingStartTime;
        private bool _voiceChatMutedPreviously = false;
        
        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(AVoiceChatManager));
        }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            DebugLite.Log("[VoiceRecordingManager] Loading...");

            _voiceChatManager = AppCore.Services.Get<AVoiceChatManager>();

            // microphone set-up
            if (Microphone.devices.Length == 0)
            {
                DebugLite.LogError("[VoiceRecordingManager] No microphone found!");
            }
            else
            {
                // Passing null to Microphone methods selects default device
                Microphone.GetDeviceCaps(null, out var minFreq, out var maxFreq);
                _Frequency = Mathf.Clamp(_Frequency, minFreq, maxFreq);
            }

            AppCore.Services.Register(this);
            DebugLite.Log("[VoiceRecordingManager] Loaded");
            return UniTask.CompletedTask;
        }

        [Button]
        public void ToggleVoiceRecording()
        {
            if (!Recording)
                StartRecording();
            else
                StopRecording();
        }
        
        [Button]
        public void StartRecording()
        {
            DebugLite.Log("[VoiceRecordingManager] Starting recording");
            
            _recordingStartTime = UnityEngine.Time.time;
            _micRecording = Microphone.Start(null, true, (int) MaxClipDuration.TotalSeconds * _MicrophoneBufferSize, _Frequency);
            
            // Todo refactor to remove dependency on VoiceChatManager
            _voiceChatMutedPreviously = _voiceChatManager.IsMicMuted();
            _voiceChatManager.SetMicMuted(Recording);
            
            RecordingStateChanged.Dispatch(Recording);
            DebugLite.Log("[VoiceRecordingManager] Recording started");
        }

        [Button]
        public void StopRecording()
        {
            DebugLite.Log("[VoiceRecordingManager] Stopping recording");
            
            Recording = false;
            Microphone.End(null);
            
            SaveVoiceRecording(_micRecording);
            
            // Todo refactor to remove dependency on VoiceChatManager
            _voiceChatManager.SetMicMuted(_voiceChatMutedPreviously);
            
            RecordingStateChanged.Dispatch(Recording);
            DebugLite.Log("[VoiceRecordingManager] Recording stopped");
        }

        private static void SaveVoiceRecording(AudioClip currRecording)
        {
            DebugLite.Log("[VoiceRecordingManager] Saving voice recording.");
            var recordingName = Path.Combine(RecordingsFolderPath, $"VoiceRecording_{DateTime.Now:yyyy-MM-dd_HH:mm:ss}");
            
            SavWav.Save(recordingName, currRecording);
        }
    }
}