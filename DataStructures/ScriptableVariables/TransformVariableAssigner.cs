// Author: František Holubec
// Created: 27.05.2025

using EDIVE.DataStructures.ScriptableVariables.Variables;
using UnityEngine;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public class TransformVariableAssigner : AVariableAssigner
    {
        [SerializeField]
        private AScriptableVariable<Transform> _Variable;

        protected override void AssignReferences()
        {
            _Variable.SetValue(transform);
        }
    }
}
