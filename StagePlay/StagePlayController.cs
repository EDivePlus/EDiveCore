// Author: František Holubec
// Created: 19.02.2026

using System;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlayController : MonoBehaviour
    {
        [SerializeField]
        private StagePlayDefinition _Definition;
        
        [SerializeReference]
        private IActivation _IncrementSegmentActivation;
        
        [SerializeReference]
        private IActivation _ResetActivation;
        
        public StagePlayDefinition Definition => _Definition;
        
        [ShowInInspector, ReadOnly]
        public StagePlayState CurrentState { get; private set; } = new();
        
        public event Action<StagePlayDefinition, StagePlayState> DefinitionChanged;

        private void Awake()
        {
            _IncrementSegmentActivation?.RegisterActivationListener(IncrementCurrentSegment);
            _ResetActivation?.RegisterActivationListener(ResetState);
            InitializeDefinition();
        }

        private void OnDestroy()
        {
            _IncrementSegmentActivation?.UnregisterActivationListener(IncrementCurrentSegment);
            _ResetActivation?.UnregisterActivationListener(ResetState);
        }

        public void SetDefinition(StagePlayDefinition definition)
        {
            if (_Definition == definition)
                return;

            _Definition = definition;
            InitializeDefinition();
            
            DefinitionChanged?.Invoke(_Definition, CurrentState);
        }

        private void InitializeDefinition()
        {
            CurrentState?.Dispose();
            CurrentState = new StagePlayState();
        }
        
        [Button]
        public void ResetState()
        {
            if (CurrentState == null)
                return;
            SetCurrentSegment(0);
        }
        
        [Button]
        public void IncrementCurrentSegment()
        {
            if (CurrentState == null)
                return;
            var newIndex = Mathf.Clamp(CurrentState.CurrentSegmentIndex + 1, 0, _Definition.ScriptSegments.Count - 1) ;
            SetCurrentSegment(newIndex);
        }
        
        [Button]
        public void SetCurrentSegment(int index)
        {
            if (CurrentState == null)
                return;
            CurrentState.CurrentSegmentIndex = index;
        }
    }
}
