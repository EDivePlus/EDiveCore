// Author: František Holubec
// Created: 20.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AAggregateCondition<T> : ABoolCondition
    {
        [SerializeField]
        [JsonProperty("Aggregation")]
        protected AggregationType _Aggregation;

        protected abstract IEnumerable<T> GetEvaluationCollection();

        protected override bool GetValue() => _Aggregation switch
        {
            AggregationType.All => GetEvaluationCollection().All(AllEvaluationMethod),
            AggregationType.Any => GetEvaluationCollection().Any(AnyEvaluationMethod),
            _ => true
        };

        protected bool AllEvaluationMethod(T value) => value == null || EvaluationMethod(value);
        protected bool AnyEvaluationMethod(T value) => value != null && EvaluationMethod(value);
        
        protected abstract bool EvaluationMethod(T value);
    }
}
