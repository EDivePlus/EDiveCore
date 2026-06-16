// Author: František Holubec
// Created: 20.10.2025

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable] 
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ABoolCondition : ABaseCondition
    {
        [SerializeField]
        [JsonProperty("Evaluation")]
        protected BoolEvaluationType _Evaluation;
        
        protected abstract bool GetValue();
        public override bool Evaluate() => GetValue() == (_Evaluation == BoolEvaluationType.IsTrue);
    }
}
