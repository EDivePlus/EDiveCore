// Author: František Holubec
// Created: 20.10.2025

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable] 
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ABoolCondition : ICondition
    {
        [SerializeField]
        [JsonProperty("Evaluation")]
        protected BoolEvaluationType _Evaluation;
        
        protected abstract bool GetValue();

        public bool Evaluate()
        {
            return GetValue() == (_Evaluation == BoolEvaluationType.IsTrue);
        }
    }
}
