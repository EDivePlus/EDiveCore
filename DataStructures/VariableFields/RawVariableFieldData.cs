// Author: František Holubec
// Created: 20.10.2025

using System;
using UnityEngine;

namespace EDIVE.DataStructures.VariableFields
{
    [Serializable]
    public class RawVariableFieldData<T> : IVariableFieldData<T>
    {
        [SerializeField]
        private T _Value;
        
        public T Value
        {
            get => _Value; 
            set => _Value = value;
        }
    }
}
