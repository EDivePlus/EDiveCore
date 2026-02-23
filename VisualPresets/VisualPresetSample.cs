// Author: František Holubec
// Created: 23.02.2026

using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets
{
    public class VisualPresetSample : MonoBehaviour
    {
        [SerializeField]
        private VisualPreset _Preset;
        
        [SerializeField]
        private VisualSwitcher _Switcher;

        [Button]
        public void Apply()
        {
            _Switcher?.Apply(_Preset);
        }
    }
}
