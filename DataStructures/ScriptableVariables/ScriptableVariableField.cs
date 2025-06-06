// Author: František Holubec
// Created: 02.06.2025

using System;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.ScriptableVariables
{
    [Serializable]
    public class ScriptableVariableField<T>
    {
        public enum TargetType
        {
            Local,
            Scriptable
        }

        [SerializeField]
        private TargetType _TargetType;

        [ShowIf("@_TargetType == TargetType.Local")]
        [SerializeField]
        private T _LocalValue;

        [ShowIf("@_TargetType == TargetType.Scriptable")]
        [SerializeField]
        private AScriptableVariable<T> _ScriptableValue;

        public T Value
        {
            get
            {
                if (_TargetType == TargetType.Local) return _LocalValue;
                if (_TargetType == TargetType.Scriptable && _ScriptableValue) return _ScriptableValue.Value;
                return default; 
            }
            set
            {
                _TargetType = TargetType.Local; 
                _LocalValue = value;
            }
        }
        public static implicit operator T(ScriptableVariableField<T> variable) => variable.Value;
    }
}
