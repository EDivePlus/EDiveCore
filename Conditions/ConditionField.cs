// Author: František Holubec
// Created: 20.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Conditions
{
    [Serializable]
    public class ConditionField
    {
        [SerializeReference] 
        [InlineProperty] 
        [HideLabel]
        private ICondition _Condition;
        
        public bool Evaluate()
        {
            return _Condition?.Evaluate() ?? true;
        }
    }
}
