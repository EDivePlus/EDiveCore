// Author: František Holubec
// Created: 27.05.2025

using EDIVE.DataStructures.ScriptableVariables.Variables;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public class TransformVariableAssigner : AVariableAssigner
    {
        [Required]
        [ShowCreateNew]
        [SerializeField]
        private AScriptableVariable<Transform> _Variable;

        protected override void AssignReferences()
        {
            _Variable.SetValue(transform);
        }
    }
}
