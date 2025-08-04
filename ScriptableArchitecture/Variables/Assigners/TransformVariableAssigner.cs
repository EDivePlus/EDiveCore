// Author: František Holubec
// Created: 27.05.2025

using EDIVE.OdinExtensions.Attributes;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables.Assigners
{
    public class TransformVariableAssigner : AScriptableAssigner
    {
        [Required]
        [ShowCreateNew]
        [SerializeField]
        private AScriptableVariable<Transform> _Variable;

        protected override void AssignReferences()
        {
            if (_Variable != null)
                _Variable.SetValue(transform);
        }

        protected override void UnassignReferences()
        {
            // OnDisable/Destroy order is not guaranteed, so we do not unassign references here.
        }
    }
}
