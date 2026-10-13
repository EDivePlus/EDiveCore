// Author: František Holubec
// Created: 13.10.2026

using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [CreateAssetMenu(menuName = "EDIVE/Visual Presets/Visual Preset Definition")]
    public class VisualPresetDefinition : AVisualPresetSource
    {
        [SerializeField]
        private VisualPreset _Preset = new();

        public override VisualPreset Preset => _Preset;

        // Call after changing records at runtime
        public void NotifyChanged() => InvokeChanged();

        private void OnValidate() => InvokeChanged();
    }
}
