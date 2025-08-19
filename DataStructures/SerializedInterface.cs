// Author: František Holubec
// Created: 19.08.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.DataStructures
{
    [Serializable]
    public class SerializedInterface<TInterface> : SerializedInterface<TInterface, Object> 
        where TInterface : class
    {
        public SerializedInterface() { }
        public SerializedInterface(TInterface value) : base(value) { }
        
        public static implicit operator TInterface(SerializedInterface<TInterface> obj) => obj.Value;
    }
    
    [Serializable]
    [InlineProperty]
    public class SerializedInterface<TInterface, TSerializedObject> : ISerializationCallbackReceiver 
        where TSerializedObject : Object 
        where TInterface : class
    {
        [SerializeField] 
        [HideInInspector]
        private TSerializedObject _SerializedValue;

        [HideLabel]
        [ShowInInspector]
        public TInterface Value
        {
            get => _SerializedValue as TInterface;
            set
            {
                _SerializedValue = value switch
                {
                    null => null,
                    TSerializedObject tObject => tObject,
                    _ => _SerializedValue
                };
            }
        }

        public TSerializedObject SerializedValue
        {
            get => _SerializedValue; 
            set => _SerializedValue = value;
        }

        public SerializedInterface() { }
        public SerializedInterface(TInterface value) => _SerializedValue = value as TSerializedObject;

        public static implicit operator TInterface(SerializedInterface<TInterface, TSerializedObject> obj) => obj.Value;
        
        public void OnBeforeSerialize()
        {
            if (_SerializedValue != null && _SerializedValue is not TInterface)
                _SerializedValue = null;
        }

        public void OnAfterDeserialize()
        {
            if (_SerializedValue != null && _SerializedValue is not TInterface)
                _SerializedValue = null;
        }
    }
}
