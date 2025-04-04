using System;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.DataStructures.TimeSpanStructures
{
    /// <summary>
    /// Unity serializable wrapper for TimeSpan, usually necessary for fields in Monobehaviour or ScriptableObject
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class UTimeSpan : ISerializationCallbackReceiver, IComparable<UTimeSpan>
    {
        [JsonProperty("TimeSpan")]
        public TimeSpan Value { get; set; }
        
        [SerializeField]
        private string _RawValue;

        [JsonConstructor]
        public UTimeSpan() => Value = TimeSpan.Zero;
        public UTimeSpan(TimeSpan value) => Value = value;

        public UTimeSpan(long ticks) : 
            this(new TimeSpan(ticks)) {}
        
        public UTimeSpan(int hours, int minutes, int seconds) : 
            this(new TimeSpan(hours, minutes, seconds)) {}
        
        public UTimeSpan(int days, int hours, int minutes, int seconds) : 
            this(new TimeSpan(days, hours, minutes, seconds)) {}

        public UTimeSpan(int days, int hours, int minutes, int seconds, int milliseconds) : 
            this(new TimeSpan(days, hours, minutes, seconds, milliseconds)) {}
        
        public int CompareTo(UTimeSpan other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Value.CompareTo(other.Value);
        }

        protected bool Equals(UTimeSpan other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UTimeSpan) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        
        public static implicit operator TimeSpan(UTimeSpan uts) => uts?.Value ?? TimeSpan.Zero;
        public static implicit operator UTimeSpan(TimeSpan ts) => new(ts);

        public void OnAfterDeserialize()
        {
            if (TimeSpan.TryParse(_RawValue, CultureInfo.InvariantCulture, out var value))
            {
                Value = value;
            }
        }

        public void OnBeforeSerialize()
        {
            _RawValue = Value.ToString();
        }

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            OnBeforeSerialize();
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            OnAfterDeserialize();
        }
    }
}
