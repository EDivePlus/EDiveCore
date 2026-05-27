// Author: Michal Petr
// Created: 27.05.2026

using System;
using EDIVE.Utils.Cysharp;
using R3;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable]
    public class PollingConditionWrapper : IObservableCondition
    {
        [SerializeReference]
        private ICondition _Condition;
        
        [SerializeField]
        private TimingPreset _Timing;
        
        private IDisposable _observingSubscription;
        
        public bool Evaluate()
        {
            return _Condition != null && _Condition.Evaluate();
        }

        public event Action StateChanged;
        public void InitializeObserving()
        {
            _observingSubscription = Observable
                .Interval(_Timing.TimeStep, _Timing.TimeProvider)
                .Select(_ => Evaluate())
                .DistinctUntilChanged()
                .Subscribe(_ => StateChanged?.Invoke());
        }

        public void TerminateObserving()
        {
            _observingSubscription?.Dispose();
        }
    }
}
