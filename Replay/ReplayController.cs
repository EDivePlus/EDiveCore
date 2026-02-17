// Author: Radim Holub
// Created: 15.07.2025

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.External.Signals;
using EDIVE.Replay.Agents;
using MemoryPack;
using MemoryPack.Compression;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Replay
{
    public class ReplayController : MonoBehaviour, IReplayController
    {
    #region Shared
        [SerializeField]
        private ReplayScope _Scope;
        
        public float CurrentDuration => Mathf.Max(CurrentTime, _currentDuration);
        public float CurrentTime { get; private set; }
        public bool HasAnyDuration => CurrentDuration > 0f;
        
        public Signal TimeChanged { get; } = new();
        public Signal StateChanged { get; } = new();

        private float _currentDuration;
        public event Action<string> RecordingSaved;

        public ReplayScope Scope => _Scope;

        private void Awake()
        {
            ResetRecording();
            UnloadPlayback();
        }

        private void OnDestroy()
        {
            ResetRecording();
            UnloadPlayback();
        }
        
        private async UniTask<byte[]> SerializeAsync<T>(T record)
        {
            await UniTask.SwitchToThreadPool();
            using var compressor = new BrotliCompressor();
            MemoryPackSerializer.Serialize(compressor, record);
            return compressor.ToArray();
        }
        
        private async UniTask<T> DeserializeAsync<T>(byte[] data)
        {
            await UniTask.SwitchToThreadPool();
            using var decompressor = new BrotliDecompressor();
            var decompressedBuffer = decompressor.Decompress(data);
            return MemoryPackSerializer.Deserialize<T>(decompressedBuffer);
        }
        
        public async UniTask<bool> LoadRecordingFromFileAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Debug.LogWarning($"Replay file not found: {path}");
                    return false;
                }

                var data = await File.ReadAllBytesAsync(path);
                ReplayRecord = await DeserializeAsync<ReplayRecord>(data);
                await UniTask.SwitchToMainThread();
                Debug.Log($"Loaded replay record: {ReplayRecord?.ID}");

                if (ReplayRecord == null)
                    return false;

                await LoadPlaybackAsync(ReplayRecord);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load replay from file '{path}': {ex.Message}");
                return false;
            }
        }
    #endregion
        
    #region Recording
        private HashSet<ReplayAgent> CurrentAgents { get; } = new();
        public bool IsRecording => _recordingCancellationTokenSource != null && !_recordingCancellationTokenSource.IsCancellationRequested;

        private CancellationTokenSource _recordingCancellationTokenSource;
        
        public ReplayRecord CreateRecord()
        {
            var id = $"{Application.productName}{DateTime.Now:yyyyMMdd-HHmmss}";
            var agentData = CurrentAgents
                .Select(a => a.GetData())
                .Where(d => d != null && !string.IsNullOrEmpty(d.ID))
                .ToList();
            
            return new ReplayRecord(id, agentData, CurrentDuration);
        }
        
        [ButtonGroup("Recording")]
        public void StartRecording()
        {
            if (IsRecording)
                StopRecordingInternal();
            _recordingCancellationTokenSource = new CancellationTokenSource();
            RecordAsync(_recordingCancellationTokenSource.Token).Forget();
        }
        
        private async UniTask RecordAsync(CancellationToken cancellationToken)
        {
            UnloadPlayback();
            if (Scope == null)
                return;
            
            // Add all currently registered agents
            foreach (var agent in Scope.Agents)
            {
                CurrentAgents.Add(agent);
            }
            
            // Listen for new agents being registered while recording
            Scope.AgentRegistered -= OnAgentRegistered;
            Scope.AgentRegistered += OnAgentRegistered;
            cancellationToken.Register(() =>
            {
                if (Scope != null) 
                    Scope.AgentRegistered -= OnAgentRegistered;
            });
            
            // Start recording from the current time
            SetRecordingTimeInternal(CurrentTime);
            foreach (var agent in CurrentAgents)
            {
                agent.StartRecording(CurrentTime, cancellationToken);
            }
            StateChanged.Dispatch();
            
            // Update the current time while recording
            while (!cancellationToken.IsCancellationRequested)
            {
                _currentDuration = CurrentTime += UnityEngine.Time.deltaTime;
                TimeChanged.Dispatch();
                await UniTask.Yield(cancellationToken);
            }
        }
        
        [ButtonGroup("Recording")]
        public void StopRecording()
        {
            StopRecordingInternal(true);
        }
        
        private bool StopRecordingInternal(bool dispatchState = false)
        {
            var wasRecording = IsRecording;
            _recordingCancellationTokenSource?.Cancel();
            _recordingCancellationTokenSource?.Dispose();
            _recordingCancellationTokenSource = null;
            if (wasRecording && dispatchState) 
                StateChanged.Dispatch();
            return wasRecording;
        }

        private void OnAgentRegistered(ReplayAgent agent)
        {
            CurrentAgents.Add(agent);
            if (IsRecording)
            {
                agent.StartRecording(CurrentTime, _recordingCancellationTokenSource.Token);
            }
        }
        
        [ButtonGroup("Recording")]
        public void ResetRecording()
        {
            StopPlaybackInternal();
            StopRecordingInternal();
            foreach (var agent in CurrentAgents)
            {
                agent.ClearData();
            }
            _currentDuration = CurrentTime = 0;
            TimeChanged.Dispatch();
            StateChanged.Dispatch();
        }
        
        public void SetRecordingTime(float time, bool clearFollowingFrames = true)
        {
            var wasPlayback = StopPlaybackInternal();
            var wasRecording = StopRecordingInternal();
            
            SetRecordingTimeInternal(time, clearFollowingFrames);
            if (wasPlayback || wasRecording)
                StateChanged.Dispatch();
        }
        
        private void SetRecordingTimeInternal(float time, bool clearFollowingFrames = true)
        {
            CurrentTime = time;
            if (clearFollowingFrames)
            {
                if (Mathf.Approximately(CurrentTime, 0))
                {
                    foreach (var agent in CurrentAgents)
                    {
                        agent.ClearData();
                    }
                }
                else
                {
                    foreach (var agent in CurrentAgents)
                    {
                        agent.ClearData(time);
                    }  
                }
                _currentDuration = time;
            }
            foreach (var agent in CurrentAgents)
            {
                agent.ApplyTime(CurrentTime);
            }
            
            TimeChanged.Dispatch();
        }
        
        private async UniTask<byte[]> ExportRecordingAsync()
        {
            if (ReplayRecord == null)
                return null;
            return await SerializeAsync(ReplayRecord);
        }
        
        public void SaveCurrentRecording()
        {
            SaveCurrentRecordingAsync().Forget();
        }
        
        public async UniTaskVoid SaveCurrentRecordingAsync()
        {
            try
            {
                if (IsRecording)
                    StopRecording();

                ReplayRecord ??= CreateRecord();

                if (ReplayRecord == null)
                {
                    Debug.LogWarning("No recording data available to save.");
                    return;
                }
                
                var sandboxPath = ReplayUtils.GetRecordingSaveFileName(ReplayRecord.ID);
                var data = await ExportRecordingAsync();
                await File.WriteAllBytesAsync(sandboxPath, data);
                await UniTask.SwitchToMainThread();

#if UNITY_ANDROID && !UNITY_EDITOR
            NativeFilePicker.ExportFile(
            sandboxPath,
            (success) =>
            {
                if (success)
                    Debug.Log($"Recording exported via NativeFilePicker from sandbox: {sandboxPath}");
                else
                    Debug.LogWarning("Recording saved in sandbox, export canceled or failed.");
                RecordingSaved?.Invoke(sandboxPath);
            }
        );
#else
                Debug.Log($"Recording saved to: {sandboxPath}");
                RecordingSaved?.Invoke(sandboxPath); 
#endif
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }


    #endregion

    #region Playback
        [SerializeField]
        private float _PlaybackSpeed = 1.0f;

        public IPlaybackContext PlaybackContext { get; private set; } = new PlaybackContext();

        public bool IsPlaybackLoaded => PlaybackLoadState == PlaybackLoadState.Loaded;
        public bool IsPlaybackLoading => PlaybackLoadState == PlaybackLoadState.Loading;
        public ReplayRecord ReplayRecord { get; private set; }
        public PlaybackLoadState PlaybackLoadState { get; private set; }
        public bool IsPlaybackPlaying => _playbackCancellationTokenSource != null && !_playbackCancellationTokenSource.IsCancellationRequested;

        private readonly List<ReplayAgentHandler> _spawnedHandlers = new();
        private readonly List<ReplayAgent> _playbackAgents = new();

        private CancellationTokenSource _playbackCancellationTokenSource;

        public void OverridePlaybackContext(IPlaybackContext context)
        {
            if (context != null)
                PlaybackContext = context;
        }
        
        [ButtonGroup("Playback")]
        public void StartPlayback()
        {
            if (IsPlaybackPlaying)
                StopPlaybackInternal();
            _playbackCancellationTokenSource = new CancellationTokenSource();
            PlaybackAsync(_playbackCancellationTokenSource.Token).Forget();
        }

        private async UniTask PlaybackAsync(CancellationToken cancellationToken)
        {
            if (Scope == null)
                return;

            StopRecordingInternal();
            if (!IsPlaybackLoaded)
                CurrentTime = 0;

            AssignCurrentRecord();
            if (ReplayRecord.Duration <= 0)
            {
                Debug.LogWarning("Cannot start playback, record duration is zero.");
                StopPlaybackInternal(true);
                return;
            }
            
            await LoadPlaybackAsync(ReplayRecord, cancellationToken);
            
            StateChanged.Dispatch();
            
            ApplyPlaybackTime(CurrentTime);
            foreach (var agent in CurrentAgents)
            {
                agent.StartPlayback(CurrentTime, cancellationToken);
            }
            
            while (!cancellationToken.IsCancellationRequested && CurrentTime >= 0 && CurrentTime < ReplayRecord.Duration)
            {
                CurrentTime = Mathf.Clamp(CurrentTime + UnityEngine.Time.deltaTime * _PlaybackSpeed, 0, ReplayRecord.Duration);
                TimeChanged.Dispatch();
                await UniTask.Yield(cancellationToken);
            }

            ApplyPlaybackTime(ReplayRecord.Duration);
            StopPlaybackInternal(true);
        }
        
        [ButtonGroup("Playback")]
        public void StopPlayback()
        {
            StopPlaybackInternal(true);
        }
        
        private bool StopPlaybackInternal(bool dispatchState = false)
        {
            var wasPlaying = IsPlaybackPlaying;
            _playbackCancellationTokenSource?.Cancel();
            _playbackCancellationTokenSource?.Dispose();
            _playbackCancellationTokenSource = null;
            if (wasPlaying && dispatchState) 
                StateChanged.Dispatch();
            return wasPlaying;
        }

        private void AssignCurrentRecord()
        {
            // Todo check if should create record 
            ReplayRecord = CreateRecord();
        }
        
        public void SetPlaybackTime(float newTime)
        {
            var stateChange = StopRecordingInternal();
            if (Scope == null) 
                return;
            
            ApplyPlaybackTime(newTime);
            if (stateChange) StateChanged.Dispatch();
        }

        private void ApplyPlaybackTime(float newTime)
        {
            CurrentTime = newTime;
            foreach (var agent in _playbackAgents)
            {
                agent.ApplyTime(CurrentTime);
            }
            TimeChanged.Dispatch();
        }

        public async UniTask LoadPlaybackAsync(ReplayRecord record, CancellationToken cancellationToken = default)
        {
            if (StopRecordingInternal())
                StateChanged.Dispatch();
            if (record == null || (ReplayRecord == record && IsPlaybackLoaded))
                return;

            ReplayRecord = record;

            if (Scope == null)
                return;

            PlaybackLoadState = PlaybackLoadState.Loading;
            StateChanged.Dispatch();

            // Prepare all agents
            var getObjectTasks = ReplayRecord.ObjectData
                .Where(data => data != null && !string.IsNullOrEmpty(data.ID))
                .Select(data => TryGetObjectAsync(data, cancellationToken));

            // All agents that are not found will be set to ignored, spawned agents are not registered in the scope
            foreach (var agent in Scope.Agents)
            {
                if (agent.CurrentPlaybackParticipation != PlaybackParticipation.Found)
                {
                    agent.SetCurrentPlaybackParticipation(PlaybackParticipation.Ignored);
                }
            }

            // Await for all tasks to complete in parallel
            var results = await UniTask.WhenAll(getObjectTasks);
            foreach (var (success, agent, data) in results)
            {
                if (!success) continue;
                agent.SetData(data);
            }
            
            // Prepare all agents for playback in parallel
            await UniTask.WhenAll(CurrentAgents.Select(a => a.PreparePlayback(CurrentTime, cancellationToken)));
            
            PlaybackLoadState = PlaybackLoadState.Loaded;
            _currentDuration = record.Duration;
            CurrentTime = Mathf.Clamp(CurrentTime, 0f, _currentDuration);
            StateChanged.Dispatch();
        }
        
        [ButtonGroup("Playback")]
        public void UnloadPlayback()
        {
            StopPlaybackInternal();
            if (PlaybackLoadState == PlaybackLoadState.NotLoaded)
                return;
            
            PlaybackLoadState = PlaybackLoadState.NotLoaded;
            CurrentTime = 0f;

            foreach (var agent in Scope.Agents)
            {
                agent.SetCurrentPlaybackParticipation(PlaybackParticipation.None);
            }
            
            // Cleanup spawned objects
            // TODO destroy only copies if resuming recording
            foreach (var handler in _spawnedHandlers)
            {
                if (handler != null)
                {
                    Destroy(handler.gameObject);
                }
            }
            _spawnedHandlers.Clear();
            _playbackAgents.Clear();
            StateChanged.Dispatch();
        }
        
        private async UniTask<(bool, ReplayAgent, ReplayAgentData)> TryGetObjectAsync(ReplayAgentData data, CancellationToken cancellationToken = default)
        {
            if (data.SpawnMode is ReplaySpawnMode.FindOnly or ReplaySpawnMode.FindOrCreate)
            {
                // Try to find existing handler
                if (Scope.TryGetAgent(data.ID, out var agent))
                {
                    _playbackAgents.Add(agent);
                    agent.SetCurrentPlaybackParticipation(PlaybackParticipation.Found);
                    return (true, agent, data);
                }
            }

            if (data.SpawnMode is ReplaySpawnMode.FindOrCreate or ReplaySpawnMode.AlwaysCreate && data.Definition != null)
            {
                var (spawned, handler) = await data.Definition.TrySpawnObjectAsync(PlaybackContext, cancellationToken);
                if (spawned)
                {
                    handler.Agent.SetCurrentPlaybackParticipation(PlaybackParticipation.Spawned);
                    handler.gameObject.name = handler.gameObject.name.Replace("(Clone)", "(Replay Clone)");
                    var agent = handler.Agent;
                    _playbackAgents.Add(agent);
                    _spawnedHandlers.Add(handler);
                    return (true, agent, data);
                }
            }

            await UniTask.CompletedTask;
            return (false, null, data);
        }
      
    #endregion
        
#if UNITY_EDITOR
        [VerticalGroup("Editor", PaddingTop = 8)]
        [ButtonGroup("Editor/File")]
        private void SaveRecordToFile()
        {
            UniTask.Void(async () =>
            {
                if (ReplayRecord == null)
                    return;
            
                var path = EditorUtility.SaveFilePanel("Save a File", "", "MyFile", "dat");
                if (string.IsNullOrEmpty(path))
                    return;

                var data = await SerializeAsync(ReplayRecord);
                await File.WriteAllBytesAsync(path, data);
                Debug.Log($"Serialized Record: {ReplayRecord.ID}");
                EditorUtility.ClearProgressBar();
            });
        }

        [ButtonGroup("Editor/File")]
        private void LoadRecordFromFile() 
        { 
            UniTask.Void(async () =>
            {
                var path = EditorUtility.OpenFilePanel("Open a File", "", "dat");
                if (string.IsNullOrEmpty(path))
                    return;

                EditorUtility.DisplayProgressBar("Serialization", "Serializing data...", 0f);
                await UniTask.SwitchToThreadPool();

                var data = await File.ReadAllBytesAsync(path);
                ReplayRecord = await DeserializeAsync<ReplayRecord>(data);
                Debug.Log($"Deserialized Record: {ReplayRecord.ID}");
                EditorUtility.ClearProgressBar();
            });
        }
#endif
    }
}
