// Author: František Holubec
// Created: 20.10.2025

using System;

namespace EDIVE.Conditions
{
    public interface IObservableCondition : ICondition
    {
        event Action StateChanged;
        
        void InitializeObserving();
        void TerminateObserving();
    }
}
