// Author: František Holubec
// Created: 08.07.2026

using System;

namespace EDIVE.XRTools.Utils.Vignette
{
    public sealed class VignetteHandle : IDisposable
    {
        private VignetteController _controller;
        private VignetteSettings _settings;
        private int _priority;

        public VignetteSettings Settings
        {
            get => _settings;
            set
            {
                if (_settings == value)
                    return;
                _settings = value;
                _controller?.ReevaluateWinner();
            }
        }

        public int Priority
        {
            get => _priority;
            set
            {
                if (_priority == value)
                    return;
                _priority = value;
                _controller?.ReevaluateWinner();
            }
        }

        public VignetteTransition Transition { get; }

        internal int Order { get; }

        internal VignetteHandle(VignetteController controller, VignetteSettings settings, int priority, VignetteTransition transition, int order)
        {
            _controller = controller;
            _settings = settings;
            _priority = priority;
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
