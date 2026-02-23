// Author: František Holubec
// Created: 19.02.2026

#if FISHNET
using FishNet.Object;
using FishNet.Object.Synchronizing;
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

        public override void OnStartClient()
        {
            base.OnStartClient();

            OnLocalDefinitionChanged(_controller.Definition, _controller.CurrentState);
            _controller.DefinitionChanged += OnLocalDefinitionChanged;

            _definition.OnChange += OnSyncDefinitionChanged;
            _currentSegment.OnChange += OnSyncCurrentSegmentChanged;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _controller.DefinitionChanged -= OnLocalDefinitionChanged;

            _definition.OnChange -= OnSyncDefinitionChanged;
            _currentSegment.OnChange -= OnSyncCurrentSegmentChanged;
        }

        private void OnLocalDefinitionChanged(StagePlayDefinition definition, StagePlayState state)
        {
            SetDefinition(definition);
            SetCurrentSegment(state.CurrentSegmentIndex);

            if (_currentState != null)
                _currentState.CurrentSegmentChanged -= OnLocalCurrentSegmentChanged;
            _currentState = state;
            _currentState.CurrentSegmentChanged += OnLocalCurrentSegmentChanged;
        }

        private void OnLocalCurrentSegmentChanged(int segment)
        {
            SetCurrentSegment(segment);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetDefinition(StagePlayDefinition definition)
        {
            _definition.Value = definition;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetCurrentSegment(int segment)
        {
            _currentSegment.Value = segment;
        }
        
        private void OnSyncCurrentSegmentChanged(int prev, int next, bool asServer)
        {
            if (asServer)
                return;
            
            if (_currentState != null) _currentState.CurrentSegmentChanged -= OnLocalCurrentSegmentChanged;
            _controller.SetCurrentSegment(next);
            if (_currentState != null) _currentState.CurrentSegmentChanged += OnLocalCurrentSegmentChanged;
        }

        private void OnSyncDefinitionChanged(StagePlayDefinition prev, StagePlayDefinition next, bool asServer)
        {
            if (asServer)
                return;
            
            _controller.DefinitionChanged -= OnLocalDefinitionChanged;
            _controller.SetDefinition(next);
            _controller.DefinitionChanged += OnLocalDefinitionChanged;
        }
    }
}
#endif
