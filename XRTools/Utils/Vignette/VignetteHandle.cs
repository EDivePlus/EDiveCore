// Author: František Holubec
// Created: 08.07.2026

using System;

namespace EDIVE.XRTools.Utils.Vignette
{
    public sealed class VignetteHandle : IDisposable
    {
        private VignetteController _controller;
        
        public VignetteSettings Settings { get; set; }
        public VignetteTransition Transition { get; }
        
        internal int Order { get; }
        public int Priority { get; set; }

        internal VignetteHandle(VignetteController controller, VignetteSettings settings, int priority, VignetteTransition transition, int order)
        {
            _controller = controller;
            Settings = settings;
            Priority = priority;
            Transition = transition;
            Order = order;
        }

        public void Dispose()
        {
            _controller?.Release(this);
            _controller = null;
        }
    }
}
