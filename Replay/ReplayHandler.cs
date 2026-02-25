// Author: František Holubec
// Created: 24.02.2026

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Replay.Agents;
using EDIVE.Replay.Strategies;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Replay
{
    [Serializable]
    public class ReplayHandler<TSpawnStrategy> : IReplayHandler where TSpawnStrategy : AReplayAgentSpawnStrategy
    {
    #region Shared
        [SerializeField]
        private ReplayScope _Scope;

        public ReplayScope Scope => _Scope;
        
        [ShowInInspector] 
        [ReadOnly] 
        public float CurrentDuration => Mathf.Max(CurrentTime, _currentDuration);

        [ShowInInspector] 
        [ReadOnly]
        [KeepRefreshing]
        public float CurrentTime { get; private set; }
        
        public bool HasAnyDuration => CurrentDuration > 0f;

        public event Action TimeChanged;
        public event Action StateChanged;

        private float _currentDuration;

        public ReplayHandler() { }
        public ReplayHandler(ReplayScope scope)
        {
            _Scope = scope;
        }

        public void Initialize()
        {
            ResetRecording();
            UnloadPlayback();
        }

        public void Terminate()
        {
            ResetRecording();
            UnloadPlayback();
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

            StateChanged?.Invoke();

            // Update the current time while recording
            while (!cancellationToken.IsCancellationRequested)
            {
                _currentDuration = CurrentTime += UnityEngine.Time.deltaTime;
                TimeChanged?.Invoke();
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
                StateChanged?.Invoke();
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
            TimeChanged?.Invoke();
            StateChanged?.Invoke();
        }

        public void SetRecordingTime(float time, bool clearFollowingFrames = true)
        {
            var wasPlayback = StopPlaybackInternal();
            var wasRecording = StopRecordingInternal();

            SetRecordingTimeInternal(time, clearFollowingFrames);
            if (wasPlayback || wasRecording)
                StateChanged?.Invoke();
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

            TimeChanged?.Invoke();
        }
    #endregion

    #region Playback
        public bool IsPlaybackLoaded => PlaybackLoadState == PlaybackLoadState.Loaded;
        public bool IsPlaybackLoading => PlaybackLoadState == PlaybackLoadState.Loading;
        public ReplayRecord ReplayRecord { get; private set; }
        [ShowInInspector] [ReadOnly]
        public PlaybackLoadState PlaybackLoadState { get; private set; }
        public bool IsPlaybackPlaying => _playbackCancellationTokenSource != null && !_playbackCancellationTokenSource.IsCancellationRequested;

        private readonly List<ReplayAgentHandler> _spawnedHandlers = new();
        private readonly List<ReplayAgent> _playbackAgents = new();

        private CancellationTokenSource _playbackCancellationTokenSource;

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
            if (!IsPlaybackLoaded || CurrentTime.Approximately(CurrentDuration))
                CurrentTime = 0;

            AssignCurrentRecord();
            if (ReplayRecord.Duration <= 0)
            {
                Debug.LogWarning("Cannot start playback, record duration is zero.");
                StopPlaybackInternal(true);
                return;
            }

            await LoadPlaybackAsync(ReplayRecord, cancellationToken);

            StateChanged?.Invoke();

            ApplyPlaybackTime(CurrentTime);
            foreach (var agent in _playbackAgents)
            {
                agent.StartPlayback(CurrentTime, cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested && CurrentTime >= 0 && CurrentTime < ReplayRecord.Duration)
            {
                CurrentTime = Mathf.Clamp(CurrentTime + UnityEngine.Time.deltaTime, 0, ReplayRecord.Duration);
                TimeChanged?.Invoke();
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
                StateChanged?.Invoke();
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
            if (stateChange) StateChanged?.Invoke();
        }

        private void ApplyPlaybackTime(float newTime)
        {
            CurrentTime = newTime;
            foreach (var agent in _playbackAgents)
            {
                agent.ApplyTime(CurrentTime);
            }

            TimeChanged?.Invoke();
        }

        public async UniTask LoadPlaybackAsync(ReplayRecord record, CancellationToken cancellationToken = default)
        {
            if (StopRecordingInternal())
                StateChanged?.Invoke();
            if (record == null || (ReplayRecord == record && IsPlaybackLoaded))
                return;

            ReplayRecord = record;

            if (Scope == null)
                return;

            PlaybackLoadState = PlaybackLoadState.Loading;
            StateChanged?.Invoke();

            // Prepare all agents
            var getObjectTasks = ReplayRecord.ObjectData
                .Where(data => data != null && !string.IsNullOrEmpty(data.ID))
                .Select(data => TryGetObjectAsync(data, cancellationToken));

            // All agents that are not found will be set to ignored, spawned agents are not registered in the scope
            foreach (var agent in Scope.Agents)
            {
                if (agent.CurrentPlaybackParticipation != PlaybackParticipation.Found) 
                    agent.SetCurrentPlaybackParticipation(PlaybackParticipation.Ignored);
            }

            // Await for all tasks to complete in parallel
            var results = await UniTask.WhenAll(getObjectTasks);
            await UniTask.Yield();
            foreach (var (success, agent, data) in results)
            {
                if (!success)
                {
                    Debug.LogWarning($"Failed to get object '{data.ID}'");
                    continue;
                }
                _playbackAgents.Add(agent);
                agent.SetData(data);
            }

            // Prepare all agents for playback in parallel
            await UniTask.WhenAll(_playbackAgents.Select(a => a.PreparePlayback(CurrentTime, cancellationToken)));

            PlaybackLoadState = PlaybackLoadState.Loaded;
            _currentDuration = record.Duration;
            CurrentTime = Mathf.Clamp(CurrentTime, 0f, _currentDuration);
            StateChanged?.Invoke();
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

            foreach (var handler in _spawnedHandlers)
            {
                if (handler != null)
                    handler.Despawn();
            }

            _spawnedHandlers.Clear();
            _playbackAgents.Clear();
            StateChanged?.Invoke();
        }

        public void SaveCurrentRecording()
        {
            throw new NotImplementedException();
        }

        private async UniTask<(bool, ReplayAgent, ReplayAgentData)> TryGetObjectAsync(ReplayAgentData data, CancellationToken cancellationToken = default)
        {
            if (data.SpawnMode is ReplaySpawnMode.FindOnly or ReplaySpawnMode.FindOrCreate)
            {
                if (Scope.TryGetAgent(data.ID, out var agent))
                {
                    agent.SetCurrentPlaybackParticipation(PlaybackParticipation.Found);
                    return (true, agent, data);
                }
            }

            if (data.SpawnMode is ReplaySpawnMode.FindOrCreate or ReplaySpawnMode.AlwaysCreate && data.Definition != null)
            {
                if (!data.Definition.TryGetStrategy<TSpawnStrategy>(out var spawnStrategy))
                {
                    Debug.LogWarning($"Replay spawn strategy '{typeof(TSpawnStrategy).Name}' not found for definition '{data.Definition.name}'");
                    return (false, null, data);
                }
                
                var (spawned, handler) = await spawnStrategy.TrySpawnObjectAsync(cancellationToken);
                if (spawned)
                {
                    handler.Agent.SetCurrentPlaybackParticipation(PlaybackParticipation.Spawned);
                    handler.gameObject.name = handler.gameObject.name.Replace("(Clone)", "(Replay Clone)");
                    var agent = handler.Agent;
                    _spawnedHandlers.Add(handler);
                    return (true, agent, data);
                }
            }

            await UniTask.CompletedTask;
            return (false, null, data);
        }
    #endregion

    #region Saving
        
        public bool IsLoadingRecord { get; private set; }
        
        public void SaveCurrentRecord()
        {
            SaveRecordingToFileAsync().Forget();
        }

        public void LoadRecord(ReplayRecordInfo info)
        {
            IsLoadingRecord = true;
            StateChanged?.Invoke();
            UniTask.Void(async () =>
            {
                await LoadRecordingFromFileAsync(ReplayUtils.GetRecordingSaveFileName(info.ID));
            });
            IsLoadingRecord = false;
            StateChanged?.Invoke();
        }

        public async UniTask<IEnumerable<ReplayRecordInfo>> GetSavedRecords()
        {
            var dir = ReplayUtils.RecordingsFolderPath;
            if (!Directory.Exists(dir))
                return Enumerable.Empty<ReplayRecordInfo>();

            var files = Directory.GetFiles(dir, "*.dat");
            var recordInfos = new List<ReplayRecordInfo>();

            foreach (var file in files)
            { 
                var fileName = Path.GetFileNameWithoutExtension(file);
                var metadataPath = $"{fileName}.meta";
                    
                if (!File.Exists(metadataPath))
                {
                    Debug.LogWarning($"Metadata file not found for file '{file}'");
                    continue; 
                }
           
                try
                {
                    var metadataJson = await File.ReadAllTextAsync(metadataPath);
                    var metadata = JsonConvert.DeserializeObject<ReplayRecordInfo>(metadataJson);
                    recordInfos.Add(metadata);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogWarning($"Failed to load metadata for file '{file}'");
                }
            }

            await UniTask.CompletedTask;
            return recordInfos;
        }
        
        private async UniTask<bool> SaveRecordingToFileAsync()
        {
            try
            {
                if (IsRecording)
                    StopRecording();

                ReplayRecord ??= CreateRecord();

                if (ReplayRecord == null)
                {
                    Debug.LogWarning("No recording data available to save.");
                    return false;
                }
                
                var sandboxPath = ReplayUtils.GetRecordingSaveFileName(ReplayRecord.ID);
                var data = await ReplayUtils.SerializeAsync(ReplayRecord);
                await File.WriteAllBytesAsync(sandboxPath, data);
                
                // Save metadata file
                var metadataPath = $"{sandboxPath}.meta";
                var metadata = new ReplayRecordInfo(ReplayRecord.ID, ReplayRecord.Duration);
                var metadataJson = JsonConvert.SerializeObject(metadata);
                await File.WriteAllTextAsync(metadataPath, metadataJson);
                
                await UniTask.SwitchToMainThread();
                Debug.Log($"Recording saved to: {sandboxPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
        
        private async UniTask<bool> LoadRecordingFromFileAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Debug.LogWarning($"Replay file not found: {path}");
                    return false;
                }

                var data = await File.ReadAllBytesAsync(path);
                ReplayRecord = await ReplayUtils.DeserializeAsync<ReplayRecord>(data);
                await UniTask.SwitchToMainThread();
                if (ReplayRecord == null)
                {
                    Debug.LogWarning($"Failed to deserialize replay record from file: {path}");
                    return false;
                }
                    
                Debug.Log($"Loaded replay record: {ReplayRecord.ID} from file: {path}");
                await LoadPlaybackAsync(ReplayRecord);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }
    #endregion
        
                
#if UNITY_EDITOR
        [VerticalGroup("Editor", PaddingTop = 8)]
        [ButtonGroup("Editor/File")]
        private async UniTask SaveRecordToFile()
        {
            if (ReplayRecord == null)
                return;
            
            var path = EditorUtility.SaveFilePanel("Save a File", "", "MyFile", "dat");
            if (string.IsNullOrEmpty(path))
                return;

            var data = await ReplayUtils.SerializeAsync(ReplayRecord);
            await File.WriteAllBytesAsync(path, data);
            Debug.Log($"Serialized Record: {ReplayRecord.ID}");
            EditorUtility.ClearProgressBar();
        }

        [ButtonGroup("Editor/File")]
        private async UniTask LoadRecordFromFile() 
        { 
            var path = EditorUtility.OpenFilePanel("Open a File", "", "dat");
            if (string.IsNullOrEmpty(path))
                return;

            EditorUtility.DisplayProgressBar("Serialization", "Serializing data...", 0f);
            await UniTask.SwitchToThreadPool();

            var data = await File.ReadAllBytesAsync(path);
            ReplayRecord = await ReplayUtils.DeserializeAsync<ReplayRecord>(data);
            Debug.Log($"Deserialized Record: {ReplayRecord.ID}");
            EditorUtility.ClearProgressBar();
        }
#endif
    }
}
