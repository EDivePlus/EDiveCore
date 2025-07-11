// Author: František Holubec
// Created: 06.05.2025

using System;
using EDIVE.External.Signals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables.Impl
{
    public abstract class AScriptableVariable : AScriptableBase
    {
        public Signal ValueChanged { get; } = new();
        public abstract bool TrySetObjectValue(object value);
        public abstract object GetObjectValue();
    }

    public abstract class AScriptableVariable<T> : AScriptableVariable
    {
        [SerializeField]
        private T _DefaultValue;

        [NonSerialized]
        private T _value;

        [NonSerialized]
        private bool _initialized;

        [ShowInInspector]
        public virtual T Value
        {
            get => GetValue();
            set => SetValue(value);
        }
        public override Type GenericType => typeof(T);

        public T GetValue()
        {
            if (_initialized)
                return _value;

            _value = _DefaultValue;
            _initialized = true;
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

        public override void ResetState()
        {
            _value = default;
            _initialized = false;
            ValueChanged.RemoveAllListeners();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
