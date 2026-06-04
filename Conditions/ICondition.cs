// Author: František Holubec
// Created: 20.10.2025

using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.Conditions
{
    [EnhancedTypeSelector(true)]
    public interface ICondition
    {
        bool Evaluate();
    }
}
