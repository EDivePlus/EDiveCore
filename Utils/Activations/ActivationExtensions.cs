// Author: František Holubec
// Created: 12.10.2026

using System;
using EDIVE.NativeUtils;

namespace EDIVE.Utils.Activations
{
    public static class ActivationExtensions
    {
        public static IDisposable RegisterDisposable(this IActivation activation, Action onActivate)
        {
            if (activation == null || onActivate == null)
                return DisposableUtils.Empty;

            activation.RegisterActivationListener(onActivate);
            return DisposableUtils.Create(() => activation.UnregisterActivationListener(onActivate));
        }
    }
}
