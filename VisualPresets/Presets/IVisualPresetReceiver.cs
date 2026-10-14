// Author: František Holubec
// Created: 14.10.2026

namespace EDIVE.VisualPresets.Presets
{
    // Takes a whole preset to use later, e.g. a tooltip that shows its texts on hover
    public interface IVisualPresetReceiver
    {
        void SetPreset(VisualPreset preset);
    }
}
