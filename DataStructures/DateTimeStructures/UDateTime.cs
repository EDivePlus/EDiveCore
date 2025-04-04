/* 
 * Source: https://gist.github.com/EntranceJew/f329f1c6a0c35ac51763455f76b5eb95
 */

using System;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.DataStructures.DateTimeStructures
{
    /// <summary>
    /// Unity serializable wrapper for DateTime, usually necessary for fields in Monobehaviour or ScriptableObject
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class UDateTime : ISerializationCallbackReceiver, IComparable<UDateTime>, IComparable<DateTime>
    {
        [JsonProperty("Value")]
        public DateTime Value { get; set; }
        
        [SerializeField]
        private string _RawValue;
        
        public DateTime UTC => Value.ToUniversalTime();
        public DateTime Local => Value.ToLocalTime();
        
        [JsonConstructor]
        public UDateTime() { }
        public UDateTime(DateTime dt) => Value = dt;

        public int CompareTo(DateTime other)
        {
            return Value.CompareTo(other);
        }

        public int CompareTo(UDateTime other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return CompareTo(other.Value);
        }

        protected bool Equals(UDateTime other)
        {
            return Value.Equals(other.Value) && Value.Kind == other.Value.Kind;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UDateTime) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => $"{Value}";

        public static implicit operator DateTime(UDateTime udt) => udt?.Value ?? default;
        public static implicit operator UDateTime(DateTime dt) => new(dt);

        public static string ConvertToString(DateTime value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        public static DateTime ConvertToDateTime(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : DateTime.MinValue;
        }

        public void OnAfterDeserialize() => Value = ConvertToDateTime(_RawValue);

        public void OnBeforeSerialize() => _RawValue = ConvertToString(Value);

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context) => OnBeforeSerialize();

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context) => OnAfterDeserialize();
    }
}
