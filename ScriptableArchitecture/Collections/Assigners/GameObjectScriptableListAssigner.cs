// Author: František Holubec
// Created: 11.07.2025

using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Collections.Assigners
{
    public class GameObjectScriptableListAssigner : AScriptableListAssigner<GameObject>
    {
        protected override bool TryGetReference(out GameObject value)
        {
            value = gameObject;
            return true;
        }
    }
}
