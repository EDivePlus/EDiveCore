// Author: František Holubec
// Created: 20.10.2025

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AComparisonCondition<T> : ABaseCondition where T: IComparable<T>
    {
        [HideLabel]
        [HorizontalGroup("Comparison")]
        [SerializeField]
        [JsonProperty("Comparison")]
        private ComparisonType _Comparison;
        
        protected abstract T CompareValue { get; }
        protected virtual IComparer<T> CustomComparer => null;
        
        protected abstract bool TryGetValue(out T value);
        
        public override bool Evaluate()
        {
            return TryGetValue(out var currentValue) && _Comparison.CompareValues(currentValue, CompareValue, CustomComparer);
        }
    }
}
