// Author: František Holubec
// Created: 29.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.View.Windows;
using EDIVE.VisualPresets.Presets;
using UnityEngine;

namespace EDIVE.Networking.ServiceHub
{
    // Something a window needs, the overlay shows its look while it is missing
    [Serializable]
    [EnhancedTypeSelector(true, 1)]
    public abstract class ARequirement
    {
        [SerializeField]
        [Tooltip("Icon and texts of the overlay")]
        private AVisualPresetSource _Look;

        [SerializeField]
        [Tooltip("Opened by the overlay button, no button when empty")]
        private WindowDefinition _FixWindow;

        private Action _changed;

        public AVisualPresetSource Look => _Look;
        public WindowDefinition FixWindow => _FixWindow;

        public abstract bool IsMet { get; }

        public void Bind(Action changed)
        {
            _changed = changed;
            OnBind();
        }

        public void Unbind()
        {
            OnUnbind();
            _changed = null;
        }

        protected abstract void OnBind();
        protected abstract void OnUnbind();

        protected void NotifyChanged() => _changed?.Invoke();
    }
}
