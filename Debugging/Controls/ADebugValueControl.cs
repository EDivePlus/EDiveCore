// Author: František Holubec
// Created: 14.10.2026

using System;
using UnityEngine;

namespace EDIVE.Debugging
{
    // Control bound to a getter and an optional setter. No setter makes it read only.
    public abstract class ADebugValueControl<TSelf, T> : ADebugControl<TSelf> where TSelf : ADebugValueControl<TSelf, T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private Func<T, bool> _validate;
        private Action<T> _onChanged;

        public bool IsReadOnly => _setter == null;

        protected ADebugValueControl(string label, Func<T> getter, Action<T> setter) : base(label)
        {
            _getter = getter;
            _setter = setter;
        }

        // Rejected values are not set, the row shows the current value again.
        public TSelf Validate(Func<T, bool> validate)
        {
            _validate = validate;
            return Self;
        }

        public TSelf OnChanged(Action<T> onChanged)
        {
            _onChanged += onChanged;
            return Self;
        }

        public T GetValue() => _getter != null ? _getter() : default;

        public void SetValue(T value)
        {
            if (_setter != null && (_validate == null || _validate(value)))
            {
                try
                {
                    _setter(value);
                    _onChanged?.Invoke(value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            Refresh();
        }
    }
}
