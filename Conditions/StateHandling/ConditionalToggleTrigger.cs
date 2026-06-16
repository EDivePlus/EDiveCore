// Author: František Holubec
// Created: 21.10.2025

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
        
        private void OnEnable()
        {
            RefreshState();
            if (_ObserveCondition)
            {
                _Condition.StateChanged += RefreshState;
                _Condition.InitializeObserving();
            }
        }

        private void OnDisable()
        {
            if (_ObserveCondition)
            {
                _Condition.StateChanged -= RefreshState;
                _Condition.TerminateObserving();
            }
        }
        
        private void RefreshState()
        {
            // If no condition, we consider it true
            var state = _Condition?.Evaluate() ?? true;
            _ToggleState.SetState(state);
        }
    }
}
