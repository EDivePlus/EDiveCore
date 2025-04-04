using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.TypeStructures
{
    [Serializable]
    public sealed class UType : ISerializationCallbackReceiver
    {
        [ShowInInspector]
        public Type Value { get; set; }
        
        [SerializeField]
        private string _RawValue;

        [JsonConstructor]
        public UType() { }
        public UType(Type value) => Value = value;

        public override string ToString() => Value?.FullName ?? "(None)";

        private bool Equals(UType other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is UType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }
        
        public static string GetClassRef(Type type)
        {
            return type != null ? type.FullName + ", " + type.Assembly.GetName().Name : "";
        }
        
        public static implicit operator Type(UType typeReference) => typeReference?.Value;
        public static implicit operator UType(Type type) => new(type);

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(_RawValue))
            {
                Value = Type.GetType(_RawValue);
                if (Value == null)
                    Debug.LogWarning($"Serialized type '{_RawValue}' not found");
            }
            else
            {
                Value = null;
            }
        }

        public void OnBeforeSerialize()
        {
            _RawValue = GetClassRef(Value);
        }
        
        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context) => OnBeforeSerialize();

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context) => OnAfterDeserialize();
    }
}
