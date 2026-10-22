// Author: Michal Petr
// Created: 22.10.2026

namespace EDIVE.XRTools.ScreenInteraction
{
    public readonly struct ScreenInteractionCommandContext
    {
        public ScreenInteractionController Controller { get; }
        public bool IsHeld { get; }

        public ScreenInteractionCommandContext(ScreenInteractionController controller, bool isHeld)
        {
            Controller = controller;
            IsHeld = isHeld;
        }
    }
}
