// Author: František Holubec
// Created: 04.09.2025

using System;

namespace EDIVE.Utils.Activations
{
    public interface IActivation
    {
        public void RegisterActivationListener(Action onActivate);
        public void UnregisterActivationListener(Action onActivate);
    }
}
