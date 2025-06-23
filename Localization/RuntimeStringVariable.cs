using System;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace EDIVE.Localization
{
    [Serializable]
    public class RuntimeStringVariable : IVariableValueChanged
    {
        private string _value;

        public event Action<IVariable> ValueChanged;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != null && _value.Equals(value))
                    return;

                _value = value;
                SendValueChangedEvent();
            }
        }

        public object GetSourceValue(ISelectorInfo _) => Value;
        private void SendValueChangedEvent() => ValueChanged?.Invoke(this);
        public override string ToString() => Value;

        public RuntimeStringVariable(string value) { _value = value; }
    }
}
