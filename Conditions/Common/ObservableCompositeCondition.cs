// Author: Michal Petr
// Created: 27.05.2026

using System;
using ZLinq;

namespace EDIVE.Conditions
{
    public class ObservableCompositeCondition : ACompositeCondition<IObservableCondition>, IObservableCondition
    {
        public event Action StateChanged;
        public void InitializeObserving()
        {
            foreach (var condition in GetEvaluationCollection().AsValueEnumerable().OfType<IObservableCondition>().Where(c => c != null))
                condition.StateChanged += OnConditionStateChanged;
        }

        public void TerminateObserving()
        {
            foreach (var condition in GetEvaluationCollection().AsValueEnumerable().OfType<IObservableCondition>().Where(c => c != null))
                condition.StateChanged -= OnConditionStateChanged;
        }
        
        private void OnConditionStateChanged() => StateChanged?.Invoke();
    }
}
