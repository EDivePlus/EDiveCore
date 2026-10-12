// Author: František Holubec
// Created: 06.06.2025

using System;
using EDIVE.Conditions;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture
{
    public abstract class AScriptableAssigner : MonoBehaviour
    {
        [SerializeReference]
        private ICondition _Condition;

        private IDisposable _conditionObserver;

        private void OnEnable()
        {
            _conditionObserver = _Condition.ObserveDisposable(OnConditionStateChanged);
            
            if (_Condition?.Evaluate() ?? true)
                AssignReferences();
        }

        private void OnConditionStateChanged()
        {
            if (_Condition.Evaluate())
                AssignReferences();
            else 
                UnassignReferences();
        }

        private void OnDisable()
        {
            _conditionObserver?.Dispose();
            _conditionObserver = null;
            UnassignReferences();
        }
        

        protected abstract void AssignReferences();
        protected abstract void UnassignReferences();
    }
}
