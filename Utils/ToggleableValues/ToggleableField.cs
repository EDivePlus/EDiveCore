using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.ToggleableValues
{
    [Serializable]
    [InlineProperty]
    public class ToggleableField<T>
    {
        [SerializeField]
        [HorizontalGroup(Width = 20)]
        [HideLabel]
        private bool _State = true;
        
        [SerializeField]
        [HorizontalGroup]
        [HideLabel]
        private T _Value;
        
        public bool State
        {
            get => _State; 
            set => _State = value;
        }
        
        public T Value
        {
            get => _Value; 
            set => _Value = value;
        }

        public static implicit operator T(ToggleableField<T> value)
        {
            return value._Value;
        }

        public ToggleableField() { }

        public ToggleableField(T value, bool state = true)
        {
            _Value = value;
            _State = state;
        }
    }
}
