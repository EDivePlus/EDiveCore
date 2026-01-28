// Author: František Holubec
// Created: 23.07.2025

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Replay.Frames;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Replay
{
    [Serializable]
    public class ReplayAgent
    {
        [Required]
        [SerializeField]
        [InlineIconButton(FontAwesomeEditorIconType.FilePenSolid, "SetObjNameID", "Set Object Name as BaseID")]
        private string _BaseID;

        [SerializeField]
        [Tooltip("Defines how the object should be spawned during replay.")]
        private ReplaySpawnMode _SpawnMode = ReplaySpawnMode.FindOrCreate;

        [HideIf(nameof(_SpawnMode), ReplaySpawnMode.FindOnly)]
        [Required("Definition is required unless SpawnMode is FindOnly.")]
        [Tooltip("Defines what object should be spawned during replay if needed")]
        [ShowCreateNew]
        [SerializeField]
        private ReplayAgentDefinition _Definition;
        
        [SerializeField]
        [ValidateMultiState("GetAllowedPlaybackParticipationStates")]
        private AMultiState _ParticipationState;

        [PropertySpace]
        [Required]
        [ShowCreateNew]
        [SerializeField]
        private ReplayRecordingConfig _RecordingConfig;
        
        [Required]
        [ShowCreateNew]
        [SerializeField]
        private ReplayScope _ReplayScope;
        
        [ReadOnly]
        [ShowInInspector]
        public PlaybackParticipation CurrentPlaybackParticipation { get; private set; }
        
        [PropertySpace]
        [SerializeField]
        [EnhancedValidate("ValidateComponents")]
        [ListDrawerSettings(DefaultExpandedState = true, OnTitleBarGUI = "OnComponentsTitleBarGUI")]
        private List<ReplayAgentComponent> _Components;

        public string BaseID => _BaseID;
        public int? DynamicID { get; set; }
        [ShowInInspector]
        public string ID => DynamicID.HasValue ? $"{_BaseID}_{DynamicID}" : _BaseID;
        
        public bool IsActive { get; private set; }

        public ReplaySpawnMode SpawnMode => _SpawnMode;
        public ReplayAgentDefinition Definition => _Definition;
        public ReplayRecordingConfig RecordingConfig => _RecordingConfig;
        public ReplayScope ReplayScope => _ReplayScope;
        public List<ReplayAgentComponent> Components => _Components;

        public float MinTime => _Components != null && _Components.Any() ? _Components.Min(s => s.MinTime) : 0f;
        public float MaxTime => _Components != null && _Components.Any() ? _Components.Max(s => s.MaxTime) : 0f;

        private CancellationTokenSource _cancellationTokenSource;
        private ReplayAgentHandler _handler;

        private List<Collider> _ownColliders;
        private IEnumerable<Collider> _enabledColliders;
        
        private List<Rigidbody> _ownRigidbodies;
        private IEnumerable<Rigidbody> _nonKinematicRigidbodies;
        
        public void Initialize(ReplayAgentHandler handler)
        {
            _handler = handler;

            if (CurrentPlaybackParticipation != PlaybackParticipation.None)
            {
                SetCurrentPlaybackParticipation(CurrentPlaybackParticipation);
            }
            
            // Register to scope only if not spawned during playback
            if (CurrentPlaybackParticipation != PlaybackParticipation.Spawned)
            {
                if (_ReplayScope != null)
                {
                    _ReplayScope.RegisterAgent(this);
                }
                else
                {
                    Debug.LogWarning($"RecordableScope not assigned to {_handler.name}. This controller will not register itself.", _handler);
                }
            }
            
            IsActive = true;
        }
        
        public void Terminate()
        {
            // Unregister from scope
            if (_ReplayScope != null)
            {
                _ReplayScope.UnregisterAgent(this);
            }
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            IsActive = false;
        }
        
        public ReplayAgentData GetData()
        {
            return new ReplayAgentData(ID, _SpawnMode, _Definition, _Components.Select(t => t.GetData()).ToList());
        }

        public void SetData(ReplayAgentData agentData)
        {
            if (agentData == null)
                return;
            
            foreach (var track in _Components)
            {
                track.SetFrames(agentData.GetTrackData(track.ID)?.FrameSequences);
            }
        }
        
        public void StartRecording(float startTime, CancellationToken cancellationToken)
        {
            if (_cancellationTokenSource != null)
            {
                Debug.LogError("Recording already in progress!");
                return;
            }
            
            if (cancellationToken.IsCancellationRequested)
                return;
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationTokenSource.Token.Register(() =>
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            });
            
            foreach (var targetRecord in _Components)
            {
                targetRecord.StartRecording(startTime, _RecordingConfig, _cancellationTokenSource.Token);
            }
        }
        
        public void ClearFrames(Predicate<AFramePreset> predicate = null)
        {
            foreach (var targetRecord in _Components)
            {
                targetRecord?.ClearFrames(predicate);
            }
        }
        
        public void ApplyTime(float value)
        {
            if (_Components == null)
                return;

            foreach (var record in _Components)
            {
                record.ApplyTime(value);
            }
        }
        
        public void SetCurrentPlaybackParticipation(PlaybackParticipation participation)
        {
            CurrentPlaybackParticipation = participation;
            if (_ParticipationState) 
                _ParticipationState.SetState(participation);
            
            if (_handler == null)
                return;
            
            if (participation == PlaybackParticipation.None)
            {
                _enabledColliders?.ForEach(c => c.enabled = true);
                _enabledColliders = null;
                
                _nonKinematicRigidbodies?.ForEach(r => r.isKinematic = false);
                _nonKinematicRigidbodies = null;
            }
            else
            {
                var isPlayback = participation is PlaybackParticipation.Found or PlaybackParticipation.Spawned;
                
                _ownColliders ??= _handler.GetComponentsInChildren<Collider>().ToList();
                _enabledColliders = _ownColliders.Where(c => c.enabled);
                if (isPlayback) 
                    _enabledColliders.ForEach(c => c.enabled = false);
                
                _ownRigidbodies ??= _handler.GetComponentsInChildren<Rigidbody>().ToList();
                _nonKinematicRigidbodies = _ownRigidbodies.Where(r => !r.isKinematic);
                if (isPlayback) 
                    _nonKinematicRigidbodies.ForEach(r => r.isKinematic = true);
            }
        }

#if UNITY_EDITOR
        [ShowInInspector]
        [PropertyRange(nameof(MinTime), nameof(MaxTime))]
        [OnValueChanged(nameof(ApplyTime))]
        private float _preview;

        
        [UsedImplicitly]
        private IEnumerable<PlaybackParticipation> GetAllowedPlaybackParticipationStates()
        {
            switch (_SpawnMode)
            {
                case ReplaySpawnMode.FindOrCreate:
                    yield return PlaybackParticipation.None;
                    yield return PlaybackParticipation.Found;
                    yield return PlaybackParticipation.Spawned;
                    break;
                case ReplaySpawnMode.FindOnly: 
                    yield return PlaybackParticipation.None;
                    yield return PlaybackParticipation.Found;
                    break;
                case ReplaySpawnMode.AlwaysCreate: 
                    yield return PlaybackParticipation.None;
                    yield return PlaybackParticipation.Spawned;
                    yield return PlaybackParticipation.Ignored;
                    break;
                default: 
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        [UsedImplicitly]
        private void OnComponentsTitleBarGUI(InspectorProperty property)
        {
            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.BrushSolid, "Clear frames"))
            {
                ClearFrames();
                property.MarkSerializationRootDirty();
            }
        }

        [UsedImplicitly]
        private void ValidateComponents(List<ReplayAgentComponent> value, SelfValidationResult result, InspectorProperty property)
        {
            if (_Components == null || _Components.Count == 0)
            {
                result.AddWarning("No tracks assigned.");
            }

            if (property.TryGetParentObject<ReplayAgentHandler>(out var handler))
            {
                var gameObject = handler.gameObject;
                if (SpawnMode != ReplaySpawnMode.FindOnly && 
                    (_Components == null || !_Components.Any(t => t.Target == gameObject && t.FrameSequences.Any(f => f is GameObjectActiveFrameSequence))))
                {
                    result.AddError("Current spawn mode requires to track main GameObject's Active state.")
                        .WithFix(() =>
                        {
                            if (!_Components.TryGetFirst(t => t.Target == gameObject, out var component))
                            {
                                component = new ReplayAgentComponent(gameObject, "GameObject");
                                _Components.Add(component);
                            }
                            component.InsertSequence(new GameObjectActiveFrameSequence());
                            property.MarkSerializationRootDirty();
                        });
                }
            }
            
            if (_Components == null)
                return;
            
            if (_Components.Any(t => t == null))
            {
                result.AddError("One or more target tracks are null.")
                    .WithFix(() =>
                    {
                        value.RemoveAll(t => t == null);
                        property.MarkSerializationRootDirty();
                    });
            }
            
            var duplicateItems = _Components.GroupBy(i => i.ID)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateItems.Count > 0)
            {
                result.AddError($"Duplicate target tracks found: {string.Join(", ", duplicateItems.Select(g => g.Key))}");
            }
        }
        
        private void SetObjNameID(InspectorProperty property)
        {
            if (property.TryGetParentObject<MonoBehaviour>(out var mono))
            {
                _BaseID = mono.gameObject.name;
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
