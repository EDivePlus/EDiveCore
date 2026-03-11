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
        
        public Color Color => _Color;

        public override string EditorLabel => "Color";

        public override bool Equals(AVisualPresetRecord other)
        {
            if (other is not ColorVisualPresetRecord colorRecord)
                return false;
            
            return base.Equals(other) && Color.Equals(colorRecord.Color);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Color);
        }
    }
}
