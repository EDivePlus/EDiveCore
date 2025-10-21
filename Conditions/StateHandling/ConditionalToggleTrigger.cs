// Author: František Holubec
// Created: 21.10.2025

using System;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Conditions.StateHandling
{
    public class ConditionalToggleTrigger : MonoBehaviour
    {
        [SerializeReference] 
        [InlineProperty] 
        [HideLabel]
        private ICondition _Condition;

        [SerializeField]
        private EvaluationTime _EvaluationTime = EvaluationTime.Awake;
        
        [SerializeField]
        private AToggleState _ToggleState;
        
        private void Awake()
        {
            if (_EvaluationTime == EvaluationTime.Awake)
                RefreshState();
        }
        
        private void OnEnable()
        {
            if (_EvaluationTime == EvaluationTime.Enable)
                RefreshState();
        }
        
        private void Start()
        {
            if (_EvaluationTime == EvaluationTime.Start)
                RefreshState();
        }
        
        private void RefreshState()
        {
            var state = Evaluate();
            _ToggleState.SetState(state);
        }
        
        private bool Evaluate()
        {
            return _Condition?.Evaluate() ?? true;
        }
        
        [Serializable]
        private enum EvaluationTime
        {
            Awake,
            Enable,
            Start
        }
    }
}
