using System;
using EDIVE.Utils.ObjectActions;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public abstract class AStateValuePreset : IObjectAction
    {
        public abstract string Title { get; }
        
        public abstract void ApplyTo(object targetObject);
        public abstract void CaptureFrom(object targetObject);
        
        public abstract Type TargetType { get; }
        public abstract bool IsValidFor(Type targetType);

        public AStateValuePreset GetCopy()
        {
            return JsonUtility.FromJson(JsonUtility.ToJson(this), GetType()) as AStateValuePreset;
        }

        protected bool Equals(AStateValuePreset other)
        {
            return other.GetType() == GetType();
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public abstract class AStateValuePreset<TTarget> : AStateValuePreset
    {
        public override void ApplyTo(object targetObject)
        {
            if (targetObject is TTarget tTarget)
                ApplyTo(tTarget);
        }
        public override void CaptureFrom(object targetObject)
        {
            if (targetObject is TTarget tTarget)
                CaptureFrom(tTarget);
        }

        public abstract void ApplyTo(TTarget targetObject);
        public abstract void CaptureFrom(TTarget targetObject);

        public override Type TargetType => typeof(TTarget);
        public override bool IsValidFor(Type targetType) => typeof(TTarget).IsAssignableFrom(targetType);
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public abstract class AStateValuePreset<TTarget, TValue> : AStateValuePreset<TTarget>
    {
        [SerializeField]
        [LabelText("$Title")]
        [JsonProperty("Value")]
        private TValue _Value;
        
        public TValue Value
        {
            get => _Value; 
            set => _Value = value;
        }

        protected AStateValuePreset() { }
        protected AStateValuePreset(TValue value)
        {
            _Value = value;
        }
    }
}
