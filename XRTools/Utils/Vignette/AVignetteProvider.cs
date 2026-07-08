// Author: František Holubec
// Created: 08.07.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.XRTools.Utils.Vignette
{
    [Serializable]
    [EnhancedTypeSelector(true)]
    public abstract class AVignetteProvider
    {
        [SerializeField]
        private int _Priority;

        protected VignetteController Controller { get; private set; }
        protected VignetteHandle Handle { get; private set; }

        public bool IsShown => Handle != null;

        public int Priority
        {
            get => _Priority;
            set
            {
                _Priority = value;
                if (Handle != null)
                    Handle.Priority = value;
            }
        }

        public void Initialize(VignetteController controller)
        {
            Controller = controller;
            OnInitialize();
        }

        public void Deinitialize()
        {
            Hide();
            OnDeinitialize();
            Controller = null;
        }

        public virtual void Tick() { }

        protected virtual void OnInitialize() { }
        protected virtual void OnDeinitialize() { }

        // Null = controller default
        protected virtual VignetteSettings GetRequestSettings() => null;
        protected virtual VignetteTransition? GetRequestTransition() => null;

        protected void Show()
        {
            if (Handle != null || Controller == null)
                return;
            var transition = GetRequestTransition();
            Handle = transition.HasValue
                ? Controller.Request(GetRequestSettings(), _Priority, transition.Value)
                : Controller.Request(GetRequestSettings(), _Priority);
        }

        protected void Hide()
        {
            Handle?.Dispose();
            Handle = null;
        }
    }
}
