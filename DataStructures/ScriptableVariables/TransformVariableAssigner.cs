// Author: František Holubec
// Created: 27.05.2025

using UnityEngine;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public class TransformVariableAssigner : MonoBehaviour
    {
        [SerializeField]
        private VariableAssignLifetime _Lifetime;

        [SerializeField]
        private TransformScriptableVariable _Variable;

        private void Awake()
        {
            if (_Lifetime == VariableAssignLifetime.Full) AssignReferences();
        }

        private void OnEnable()
        {
            if (_Lifetime == VariableAssignLifetime.Enabled) AssignReferences();
        }

        private void AssignReferences()
        {
            _Variable.SetValue(transform);
            
        }
    }
}
