// Author: František Holubec
// Created: 06.05.2025

using System;
using EDIVE.External.DomainReloadHelper;
using EDIVE.External.Signals;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.DataStructures.ScriptableVariables
{
    public abstract class AScriptableVariable : ScriptableObject
    {
        public abstract Type VariableType { get; }
        public Signal ValueChanged { get; } = new();

        public abstract void Clear();

        public abstract bool TrySetObjectValue(object value);
        public abstract object GetObjectValue();

#if UNITY_EDITOR
        [ExecuteOnReload(-1000)]
        private static void OnReload()
        {
            var variables = EditorAssetUtils.FindAllAssetsOfType<AScriptableVariable>();
            foreach (var variable in variables)
            {
                variable.Clear();
            }
        }
#endif
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

#if UNITY_EDITOR
        [ExecuteOnReload(-1000)]
        private static void OnReload()
        {
            var variables = EditorAssetUtils.FindAllAssetsOfType<AScriptableVariable>();
            foreach (var variable in variables)
            {
                variable.Clear();
            }
        }
#endif
    }
}
