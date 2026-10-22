// Author: Michal Petr
// Created: 22.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using UnityEngine;

namespace EDIVE.XRTools.ScreenInteraction
{
    public enum ScreenInteractionCommandSlot
    {
        Targeted,
        Held,
        Both
    }

    // One button of a ScreenInteractable, override what the command needs
    [Serializable]
    [EnhancedTypeSelector(true, 1)]
    public abstract class AScreenInteractionCommand
    {
        [SerializeField]
        private VisualPreset _Visual = new();

        public VisualPreset Visual => _Visual;

        public virtual ScreenInteractionCommandSlot ShownWhen => ScreenInteractionCommandSlot.Held;
        public virtual bool IsHold => false;
        public virtual bool IsVisible => true;
        public virtual bool IsUnlocked => true;

        public bool IsShown(ScreenInteractionCommandContext context)
        {
            var slot = context.IsHeld ? ScreenInteractionCommandSlot.Held : ScreenInteractionCommandSlot.Targeted;
            return (ShownWhen == slot || ShownWhen == ScreenInteractionCommandSlot.Both) && IsVisible;
        }

        public abstract UniTask Execute(ScreenInteractionCommandContext context, CancellationToken cancellationToken);

        // Calls back when visibility or lock may have changed
        public virtual IDisposable Observe(Action onChanged) => DisposableUtils.Empty;
    }
}
