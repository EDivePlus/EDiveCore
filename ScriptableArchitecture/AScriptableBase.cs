// Author: František Holubec
// Created: 11.07.2025

using System;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture
{
    public abstract class AScriptableBase : ScriptableObject
    {
        public abstract Type GenericType { get; }
        public virtual void ResetState() { }
    }
}
