// Author: František Holubec
// Created: 18.02.2026

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EDIVE.Utils.Activations
{
    [Serializable]
    [MovedFrom(true, "EDIVE.Utils.Activations", "EDIVE.Utils")]
    public class CompoundActivation : IActivation
    {
        [SerializeReference]
        private List<IActivation> _Activations = new();

        public void RegisterActivationListener(Action onActivate)
        {
            foreach (var activation in _Activations)
                activation?.RegisterActivationListener(onActivate);
        }

        public void UnregisterActivationListener(Action onActivate)
        {
            foreach (var activation in _Activations)
                activation?.UnregisterActivationListener(onActivate);
        }
    }
}
