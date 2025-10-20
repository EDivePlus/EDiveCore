// Author: František Holubec
// Created: 20.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
    
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ACompositeCondition<T> : ABoolCondition
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
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class CompositeCondition : ACompositeCondition<ICondition>
    {
        [InlineProperty]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [JsonProperty("Conditions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        [ListDrawerSettings(ShowFoldout = false, OnBeginListElementGUI = "DrawConditionTitle")]
        private List<ICondition> _Conditions = new();

        protected override IEnumerable<ICondition> GetEvaluationCollection() => _Conditions;
        protected override bool EvaluationMethod(ICondition value) => value.Evaluate();

#if UNITY_EDITOR
        [UsedImplicitly]
        private void DrawConditionTitle(int index)
        {
            if (index > 0) 
                GUILayout.Label($"- {_Aggregation.ToOperatorString()} -", SirenixGUIStyles.LeftAlignedGreyMiniLabel);
        }
#endif
    }
}
