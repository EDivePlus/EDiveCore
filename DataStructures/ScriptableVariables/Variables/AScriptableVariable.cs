// Author: František Holubec
// Created: 06.05.2025

using System;
using EDIVE.External.Signals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.ScriptableVariables.Variables
{
    public abstract class AScriptableVariable : ScriptableObject
    {
        public abstract Type VariableType { get; }
        public Signal ValueChanged { get; } = new();

        public abstract void Clear();

        public abstract bool TrySetObjectValue(object value);
        public abstract object GetObjectValue();
    }

    public abstract class AScriptableVariable<T> : AScriptableVariable
    {
        [NonSerialized]
        private T _value;

        [ShowInInspector]
        public virtual T Value
        {
            get => GetValue();
            set => SetValue(value);
        }
        public override Type VariableType => typeof(T);

        public T GetValue()
        {
            return _value;
        }

        public void SetValue(T value)
        {
            if (Equals(value, _value))
                return;

            var prev = _value;
            _value = value;
            OnValueChanged(prev, value);
        }

        public override bool TrySetObjectValue(object value)
        {
            if (value is not T tValue)
                return false;

            SetValue(tValue);
            return true;
        }

        public override object GetObjectValue()
        {
            return GetValue();
        }

        protected virtual void OnValueChanged(T prev, T current)
        {
            ValueChanged.Dispatch();
        }

        public static implicit operator T(AScriptableVariable<T> variable) => variable.Value;

        public override void Clear()
        {
            _value = default;
            ValueChanged.RemoveAllListeners();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
