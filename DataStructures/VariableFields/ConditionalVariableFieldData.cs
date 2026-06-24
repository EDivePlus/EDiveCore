// Author: Michal Petr
// Created: 24.06.2026

using System;
using EDIVE.Conditions;
using UnityEngine;

namespace EDIVE.DataStructures.VariableFields
{
    [Serializable]
    public class ConditionalVariableFieldData<T> : IVariableFieldData<T>
    {
        [SerializeReference]
        private ICondition _Condition;
        
        [SerializeField]
        private VariableField<T> _ConditionalValue;

        [SerializeField]
        private VariableField<T> _DefaultValue;

        public T Value
        {
            get => _Condition != null && _Condition.Evaluate() ? _ConditionalValue.Value : _DefaultValue.Value;
            set
            {
                if (_Condition != null && _Condition.Evaluate())
                {
                    _ConditionalValue.Value = value;
                }
                else
                {
                    _DefaultValue.Value = value;
                }
            }
        }
    }
}
