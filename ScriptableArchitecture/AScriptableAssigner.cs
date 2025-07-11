// Author: František Holubec
// Created: 06.06.2025

using UnityEngine;

namespace EDIVE.ScriptableArchitecture
{
    public abstract class AScriptableAssigner : MonoBehaviour
    {
        [SerializeField]
        private AssignLifetime _Lifetime;

        private void Awake()
        {
            if (_Lifetime == AssignLifetime.Full)
                AssignReferences();
        }

        private void OnEnable()
        {
            if (_Lifetime == AssignLifetime.Enabled)
                AssignReferences();
        }

        private void OnDisable()
        {
            if (_Lifetime == AssignLifetime.Enabled)
                UnassignReferences();
        }

        private void OnDestroy()
        {
            if (_Lifetime == AssignLifetime.Full)
                UnassignReferences();
        }

        protected abstract void AssignReferences();
        protected abstract void UnassignReferences();
    }
}
