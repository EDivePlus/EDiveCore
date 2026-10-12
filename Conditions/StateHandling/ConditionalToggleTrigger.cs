// Author: František Holubec
// Created: 21.10.2025

using System;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;

namespace EDIVE.Conditions.StateHandling
{
    public class ConditionalToggleTrigger : MonoBehaviour
    {
        [SerializeReference] 
        private ICondition _Condition;
        
        [SerializeField]
        private bool _ObserveCondition = true;
        
        [SerializeField]
        private AToggleState _ToggleState;

        private IDisposable _conditionObserver;
        
        private void OnEnable()
        {
            RefreshState();
            if (_ObserveCondition)
                _conditionObserver = _Condition.ObserveDisposable(RefreshState);
        }

        private void OnDisable()
        {
            _conditionObserver?.Dispose();
            _conditionObserver = null;
        }
        
        private void RefreshState()
        {
            // If no condition, we consider it true
            var state = _Condition?.Evaluate() ?? true;
            _ToggleState.SetState(state);
        }
    }
}
