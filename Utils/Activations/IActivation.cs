// Author: František Holubec
// Created: 04.09.2025

using System;
using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.Utils.Activations
{
    [EnhancedTypeSelector(true)]
    public interface IActivation
    {
        public void RegisterActivationListener(Action onActivate);
        public void UnregisterActivationListener(Action onActivate);
    }
}
