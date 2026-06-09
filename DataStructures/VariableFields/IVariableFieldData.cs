// Author: František Holubec
// Created: 20.10.2025

using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.DataStructures.VariableFields
{
    [EnhancedTypeSelector(true, 1)]
    public interface IVariableFieldData<T>
    {
        T Value { get; set; }
    }
}
