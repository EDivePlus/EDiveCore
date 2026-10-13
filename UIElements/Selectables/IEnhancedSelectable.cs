// Author: František Holubec
// Created: 13.10.2026

using EDIVE.VisualPresets.Switchers;

namespace EDIVE.UIElements.Selectables
{
    // RefreshVisuals replays the current state instantly
    public interface IEnhancedSelectable : IVisualPresetRefreshable
    {
        SelectableAdditionalData AdditionalData { get; }
    }
}
