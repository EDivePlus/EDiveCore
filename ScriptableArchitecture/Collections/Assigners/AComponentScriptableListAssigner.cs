// Author: František Holubec
// Created: 11.07.2025

using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Collections.Assigners
{
    public class AComponentScriptableListAssigner<T> : AScriptableListAssigner<T> where T : Component
    {
        protected override bool TryGetReference(out T value)
        {
            return gameObject.TryGetComponent(out value);
        }
    }
}
