// Author: František Holubec
// Created: 20.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.VariableFields
{
    [Serializable]
    [TypeRegistryItem("Raw Data")]
    public class RawVariableFieldData<T> : IVariableFieldData<T>
    {
        [HideLabel]
        [SerializeField]
        private T _Value;
        
        public T Value
        {
            get => _Value; 
            set => _Value = value;
        }
    }
}
