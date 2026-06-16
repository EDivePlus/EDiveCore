// Author: František Holubec
// Created: 20.10.2025

using System;
using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.Conditions
{
    [EnhancedTypeSelector(true, 1)]
    public interface ICondition
    {
        bool Evaluate();
        
        event Action StateChanged;
        void InitializeObserving();
        void TerminateObserving();
    }
}
