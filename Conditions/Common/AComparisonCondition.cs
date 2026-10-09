// Author: František Holubec
// Created: 20.10.2025

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AComparisonCondition<T> : ABaseCondition
    {
        [HideLabel]
        [HorizontalGroup("Comparison")]
        [SerializeField]
        [JsonProperty("Comparison")]
        private ComparisonType _Comparison;
        
        protected abstract T CompareValue { get; }
        // Override for custom ordering
        protected virtual int Compare(T a, T b) => Comparer<T>.Default.Compare(a, b);
        
        protected abstract bool TryGetValue(out T value);
        
        public override bool Evaluate()
        {
            return TryGetValue(out var currentValue) && _Comparison.Matches(Compare(currentValue, CompareValue));
        }
    }
}
