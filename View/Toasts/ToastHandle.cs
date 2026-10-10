// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.VisualPresets.Presets;

namespace EDIVE.View.Toasts
{
    // One shown or queued toast, dispose dismisses it
    public class ToastHandle : IDisposable
    {
        private readonly ToastManager _manager;

        public VisualPreset Preset { get; }

        // Zero or less stays until dismissed
        public float Duration { get; }

        public bool IsDismissed { get; internal set; }

        internal ToastDisplay Display { get; set; }
        internal float Remaining { get; set; }
        internal bool IsHiding { get; set; }

        internal ToastHandle(ToastManager manager, VisualPreset preset, float duration)
        {
            _manager = manager;
            Preset = preset;
            Duration = duration;
            Remaining = duration;
        }

        public void Dismiss()
        {
            if (_manager != null)
                _manager.Dismiss(this);
        }

        public void Dispose() => Dismiss();
    }
}
