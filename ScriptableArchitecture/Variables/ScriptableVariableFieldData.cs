// Author: František Holubec
// Created: 20.10.2025

using System;
using EDIVE.DataStructures.VariableFields;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables
{
    [Serializable]
    [TypeRegistryItem("Scriptable Variable")]
    public class ScriptableVariableFieldData<T> : IVariableFieldData<T>
    {
        [SerializeField]
        [EnhancedAssetSelector]
        private AScriptableVariable<T> _Variable;

        public T Value
        {
            get => _Variable != null ? _Variable.Value : default;
            set
            {
                if (_Variable != null) 
                    _Variable.Value = value;
            }
            
        }
    }
}
