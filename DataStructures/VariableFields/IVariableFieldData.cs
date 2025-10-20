// Author: František Holubec
// Created: 20.10.2025

namespace EDIVE.DataStructures.VariableFields
{
    public interface IVariableFieldData<T>
    {
        T Value { get; set; }
    }
}
