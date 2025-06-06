// Author: František Holubec
// Created: 06.06.2025

using EDIVE.DataStructures.ScriptableVariables.Variables;
using UnityEngine;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public abstract class AVariableAssigner : MonoBehaviour
    {
        [SerializeField]
        private VariableAssignLifetime _Lifetime;

        private void Awake()
        {
            if (_Lifetime == VariableAssignLifetime.Full)
            {
                AssignReferences();
            }
        }

        private void OnEnable()
        {
            if (_Lifetime == VariableAssignLifetime.Enabled)
            {
                AssignReferences();
            }
        }

        protected abstract void AssignReferences();
    }
}
