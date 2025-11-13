// Author: František Holubec
// Created: 20.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.VariableFields
{
    [Serializable]
    [InlineProperty]
    public class VariableField<T>
    {
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        private IVariableFieldData<T> _Data = new RawVariableFieldData<T>();
        
        public IVariableFieldData<T> Data => _Data ??= new RawVariableFieldData<T>();
        
        public T Value
        {
            get => Data.Value;
            set => Data.Value = value;
        }
    }
}
