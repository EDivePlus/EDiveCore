// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public class ColorVisualPresetRecord : AVisualPresetRecord<ColorVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Color _Color;
        
        public Color Color
        {
            get => _Color;
            set => _Color = value;
        }
        
        public override string EditorLabel => "Color";

    }
}
