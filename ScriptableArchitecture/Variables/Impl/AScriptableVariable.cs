// Author: František Holubec
// Created: 06.05.2025

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables.Impl
{
    public abstract class AScriptableVariable : AScriptableBase
    {
        public event Action ValueChanged;
        public abstract bool TrySetObjectValue(object value);
        public abstract object GetObjectValue();
        
        protected void InvokeValueChanged() => ValueChanged?.Invoke();
        protected void ClearValueChangedEvent() => ValueChanged = null;
    }

    public abstract class AScriptableVariable<T> : AScriptableVariable
    {
        [SerializeField]
        private bool _IsReadOnly;
        
        [EnhancedInlineProperty]
        [SerializeField]
        private T _DefaultValue;
        
        [NonSerialized]
        private T _value;
        
        [NonSerialized]
        private bool _initialized;

        [EnhancedInlineProperty]
        [HideIf(nameof(_IsReadOnly))]
        [ShowInInspector]
        public virtual T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public bool IsReadOnly => _IsReadOnly;
        public override Type GenericType => typeof(T);

        public T GetValue()
        {
            if (_IsReadOnly) 
                return _DefaultValue;
            
            if (_initialized)
                return _value;

            _value = _DefaultValue;
            _initialized = true;
            return _value;
        }

        public void SetValue(T value)
        {
            if (_IsReadOnly)
            {
                Debug.LogError($"Trying to set value of read-only variable {name}.");
                return;
            }
            
            if (Equals(value, _value))
                return;
            
            var prev = _value;
            _value = value;
            _initialized = true;
            OnValueChanged(prev, value);
        }

        public override bool TrySetObjectValue(object value)
        {
            if (_IsReadOnly)
            {
                Debug.LogError($"Trying to set value of read-only variable {name}.");
                return false;
            }
            
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
            InvokeValueChanged();
        }

        public static implicit operator T(AScriptableVariable<T> variable) => variable.Value;

        public override void ResetState()
        {
            _value = default;
            _initialized = false;
            ClearValueChangedEvent();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
