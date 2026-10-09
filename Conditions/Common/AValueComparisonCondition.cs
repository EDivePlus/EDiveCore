// Author: František Holubec
// Created: 20.10.2025

using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AValueComparisonCondition<T> : AComparisonCondition<T>
    {
        [HideLabel] 
        [HorizontalGroup("Comparison")]
        [JsonProperty("CompareValue")] 
        [SerializeField]
        private T _CompareValue;

        protected AValueComparisonCondition() { }

        protected AValueComparisonCondition(T compareValue)
        {
            _CompareValue = compareValue;
        }

        protected override T CompareValue => _CompareValue;
    }
}
