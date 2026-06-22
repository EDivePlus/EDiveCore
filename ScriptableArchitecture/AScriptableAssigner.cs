// Author: František Holubec
// Created: 06.06.2025

using EDIVE.Conditions;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture
{
    public abstract class AScriptableAssigner : MonoBehaviour
    {
        [SerializeReference]
        private ICondition _Condition;

        private void OnEnable()
        {
            if (_Condition != null)
            {
                _Condition.InitializeObserving();
                _Condition.StateChanged += OnConditionStateChanged;
            }
            
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
            if (_Condition != null)
            {
                _Condition.TerminateObserving();
                _Condition.StateChanged -= OnConditionStateChanged;
            }
            UnassignReferences();
        }
        

        protected abstract void AssignReferences();
        protected abstract void UnassignReferences();
    }
}
