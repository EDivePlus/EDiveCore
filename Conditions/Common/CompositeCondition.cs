// Author: Michal Petr
// Created: 27.05.2026

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using ZLinq;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ACompositeCondition<TCondition> : AAggregateCondition<TCondition> where TCondition : ICondition
    {
        [InlineProperty]
        [SerializeReference]
        [HideReferenceObjectPicker]
        [JsonProperty("Conditions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        [ListDrawerSettings(ShowFoldout = false, OnBeginListElementGUI = "DrawConditionTitle")]
        private List<TCondition> _Conditions = new();

        protected override IEnumerable<TCondition> GetEvaluationCollection() => _Conditions ?? Enumerable.Empty<TCondition>();
        protected override bool EvaluationMethod(TCondition value) => value.Evaluate();
        
        public override void InitializeObserving()
        {
            foreach (var condition in GetEvaluationCollection().AsValueEnumerable().OfType<ICondition>().Where(c => c != null))
                condition.StateChanged += OnConditionStateChanged;
        }

        public override void TerminateObserving()
        {
            foreach (var condition in GetEvaluationCollection().AsValueEnumerable().OfType<ICondition>().Where(c => c != null))
                condition.StateChanged -= OnConditionStateChanged;
        }
        
        private void OnConditionStateChanged() => InvokeStateChanged();
        
#if UNITY_EDITOR
        [UsedImplicitly]
        private void DrawConditionTitle(int index)
        {
            if (index > 0) 
                GUILayout.Label($"- {_Aggregation.ToOperatorString()} -", SirenixGUIStyles.LeftAlignedGreyMiniLabel);
        }
#endif
    }
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class CompositeCondition : ACompositeCondition<ICondition>
    {
        
    }
}
