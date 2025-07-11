// Author: František Holubec
// Created: 11.07.2025

using EDIVE.ScriptableArchitecture.Collections.Impl;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Collections.Assigners
{
    public abstract class AScriptableListAssigner<T> : AScriptableAssigner
    {
        [SerializeField]
        private AScriptableList<T> _ScriptableList;

        protected override void AssignReferences()
        {
            if (TryGetReference(out var value))
            {
                _ScriptableList.TryAdd(value);
            }
        }

        protected override void UnassignReferences()
        {
            if (TryGetReference(out var value))
            {
                _ScriptableList.Remove(value);
            }
        }

        protected abstract bool TryGetReference(out T value);
    }
}
