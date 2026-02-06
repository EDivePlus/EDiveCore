using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Time.TimeSpanUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Audio.VoiceRecording
{
    public class VoiceRecordingManager : ALoadableServiceBehaviour<VoiceRecordingManager>
    {
        [TimeSpanDrawerSettings(TimeUnit.Minutes)]
        [SerializeField]
        private UTimeSpan _MaxClipDuration = TimeSpan.FromSeconds(60);

        [ShowInInspector]
        [KeepRefreshing]
        [TimeSpanDrawerSettings(TimeUnit.Minutes)]
        public TimeSpan CurrentRecordingTime => Recording ? TimeSpan.FromSeconds(UnityEngine.Time.time - _recordingStartTime) : TimeSpan.Zero;
        
        public TimeSpan MaxClipDuration => _MaxClipDuration;
        
        [ShowInInspector][ReadOnly]
        public bool Recording { get; private set; }

        private static string RecordingsFolderPath => PathUtility.GetRootAppDataPath("VoiceRecordings");
        
        public Signal<bool> RecordingStateChanged { get; } = new();

        private AudioManager _audioManager;
        private AudioClip _micRecording;
        private float _recordingStartTime;
        private CancellationTokenSource _recordingCancellation = new();
        private readonly List<AudioFrame> _recordedFrames = new();
        
        private readonly List<IAudioFilter> _audioFilters = new();
        
        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _audioManager = AppCore.Services.Get<AudioManager>();
            
            _audioFilters.Add(new RNNoiseFilter()); // Noise suppression
            _audioFilters.Add(new SimpleVadFilter(new SimpleVad())); // Voice activity detection
            
            return UniTask.CompletedTask;
        }
              
        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(AudioManager));
        }
        
        [Button]
        public void StartRecording()
        {
            if (Recording)
            {
                DebugLite.Log("[VoiceRecordingManager] Recording is already in progress");
                return;
            }
            
            _recordedFrames.Clear();
            _audioManager.AddVoiceChatMuteRequest(this);
            _audioManager.LocalAudioFrameReady += OnAudioFrameReady;
            
            DebugLite.Log("[VoiceRecordingManager] Starting recording");
            Recording = true;
            _recordingStartTime = UnityEngine.Time.time;
            
            RecordingStateChanged.Dispatch(Recording);
            DebugLite.Log("[VoiceRecordingManager] Recording started");
            
            _recordingCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RecordingTask(_recordingCancellation.Token).Forget();
        }

        private void OnAudioFrameReady(AudioFrame frame)
        {
            if (_audioFilters != null) {
                foreach (var filter in _audioFilters) {
                    frame = filter.Run(frame);
                    if (frame.samples == null)
                        break;
                }
            }

            if (frame.samples != null && frame.samples.Length > 0)
                _recordedFrames.Add(frame);
        }

        private async UniTask RecordingTask(CancellationToken ct)
        {
            await UniTask.Delay(MaxClipDuration, DelayType.Realtime, cancellationToken: ct);
            StopRecording();
        }
        
        [Button]
        public void StopRecording()
        {
            DebugLite.Log("[VoiceRecordingManager] Stopping recording");
            
            Recording = false;
            _recordingCancellation.Cancel();
            _recordingCancellation.Dispose();
            _recordingCancellation = null;
            
            _audioManager.LocalAudioFrameReady -= OnAudioFrameReady;
            _audioManager.RemoveVoiceChatMuteRequest(this);

            _micRecording = AudioUtils.CreateAudioClip(_recordedFrames);
            _recordedFrames.Clear();
            SaveVoiceRecording(_micRecording);
            
            RecordingStateChanged.Dispatch(Recording);
            DebugLite.Log("[VoiceRecordingManager] Recording stopped");
        }

        private static void SaveVoiceRecording(AudioClip currRecording)
        {
            DebugLite.Log("[VoiceRecordingManager] Saving voice recording.");
            var recordingName = Path.Combine(RecordingsFolderPath, $"VoiceRecording_{DateTime.Now:yyyyMMddHHmmss}");
            PathUtility.EnsurePathExists(RecordingsFolderPath);
            SavWav.Save(recordingName, currRecording);
        }
    }
}