// Author: František Holubec
// Created: 16.06.2026

using System;

namespace EDIVE.Conditions
{
    public abstract class ABaseCondition : ICondition
    {
        public event Action StateChanged;
        
        public abstract bool Evaluate();  
        
        public virtual void InitializeObserving(){ }
        public virtual void TerminateObserving(){ }
        
        protected void InvokeStateChanged() => StateChanged?.Invoke();
    }
}
