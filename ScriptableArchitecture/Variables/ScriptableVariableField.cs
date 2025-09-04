// Author: František Holubec
// Created: 02.06.2025

using System;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables
{
    [Serializable]
    [InlineProperty]
    public class ScriptableVariableField<T>
    {
        [HorizontalGroup]
        [HideLabel]
        [SerializeField]
        private ScriptableVariableTargetType _TargetType;

        [HorizontalGroup]
        [HideLabel]
        [ShowIf("@_TargetType == ScriptableVariableTargetType.Local")]
        [SerializeField]
        private T _LocalValue;

        [HorizontalGroup]
        [HideLabel]
        [ShowIf("@_TargetType == ScriptableVariableTargetType.Scriptable")]
        [SerializeField]
        private AScriptableVariable<T> _ScriptableValue;

        public T Value
        {
            get
            {
                if (_TargetType == ScriptableVariableTargetType.Local) return _LocalValue;
                if (_TargetType == ScriptableVariableTargetType.Scriptable && _ScriptableValue) return _ScriptableValue.Value;
                return default; 
            }
            set
            {
                _TargetType = ScriptableVariableTargetType.Local; 
                _LocalValue = value;
            }
        }

        public ScriptableVariableField() { }
        
        public ScriptableVariableField(ScriptableVariableTargetType targetType)
        {
            _TargetType = targetType;
        }

        public ScriptableVariableField(AScriptableVariable<T> scriptableValue)
        {
            _ScriptableValue = scriptableValue;
            _TargetType = ScriptableVariableTargetType.Scriptable;
        }

        public ScriptableVariableField(T localValue)
        {
            _LocalValue = localValue;
            _TargetType = ScriptableVariableTargetType.Local;
        }

        public static implicit operator T(ScriptableVariableField<T> variable) => variable.Value;
    }
    
    public enum ScriptableVariableTargetType
    {
        Local,
        Scriptable
    }
}
