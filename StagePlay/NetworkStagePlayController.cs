// Author: František Holubec
// Created: 19.02.2026

#if PURRNET
using PurrNet;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [RequireComponent(typeof(StagePlayController))]
    public class NetworkStagePlayController : NetworkBehaviour
    {
        private StagePlayController _controller;

        private readonly SyncVar<StagePlayDefinition> _definition = new();
        private readonly SyncVar<int> _currentSegment = new();

        private StagePlayState _currentState;

        private void Awake()
        {
            _controller = GetComponent<StagePlayController>();
        }

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer)
                return;

            _definition.value = _controller.Definition;
            if (_controller.CurrentState != null)
                _currentSegment.value = _controller.CurrentState.CurrentSegmentIndex;
        }

        protected override void OnSpawned()
        {
            // Don't push local state on spawn, late joiner would reset everyone
            BindState(_controller.CurrentState);
            _controller.DefinitionChanged += OnLocalDefinitionChanged;
            _definition.onChanged += OnSyncDefinitionChanged;
            _currentSegment.onChanged += OnSyncCurrentSegmentChanged;
            if (!isServer)
            {
                OnSyncDefinitionChanged(_definition.value);
                OnSyncCurrentSegmentChanged(_currentSegment.value);
            }
        }
        protected override void OnDespawned()
        {
            _controller.DefinitionChanged -= OnLocalDefinitionChanged;
            BindState(null);
            _definition.onChanged -= OnSyncDefinitionChanged;
            _currentSegment.onChanged -= OnSyncCurrentSegmentChanged;
        }
        private void BindState(StagePlayState state)
        {
            if (_currentState != null)
                _currentState.CurrentSegmentChanged -= OnLocalCurrentSegmentChanged;
            _currentState = state;
            if (_currentState != null)
                _currentState.CurrentSegmentChanged += OnLocalCurrentSegmentChanged;
        }
        private void OnLocalDefinitionChanged(StagePlayDefinition definition, StagePlayState state)
        {
            SetDefinition(definition);
            BindState(state);
            if (state != null)
                SetCurrentSegment(state.CurrentSegmentIndex);
        }

        private void OnLocalCurrentSegmentChanged(int segment)
        {
            SetCurrentSegment(segment);
        }

        [ServerRpc(requireOwnership: false)]
        private void SetDefinition(StagePlayDefinition definition)
        {
            _definition.value = definition;
        }

        [ServerRpc(requireOwnership: false)]
        private void SetCurrentSegment(int segment)
        {
            _currentSegment.value = segment;
        }

        private void OnSyncCurrentSegmentChanged(int next)
        {
            if (_currentState != null) _currentState.CurrentSegmentChanged -= OnLocalCurrentSegmentChanged;
            _controller.SetCurrentSegment(next);
            if (_currentState != null) _currentState.CurrentSegmentChanged += OnLocalCurrentSegmentChanged;
        }

        private void OnSyncDefinitionChanged(StagePlayDefinition next)
        {
            _controller.DefinitionChanged -= OnLocalDefinitionChanged;
            _controller.SetDefinition(next);
            _controller.DefinitionChanged += OnLocalDefinitionChanged;
            // SetDefinition makes new state
            BindState(_controller.CurrentState);
        }
    }
}
#endif
