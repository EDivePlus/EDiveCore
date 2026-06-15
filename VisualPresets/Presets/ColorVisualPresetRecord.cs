// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.Utils.Json.TypeNames;
using EDIVE.VisualPresets.VisualIDs;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [JsonTypeName("VisualPreset.Color")]
    public class ColorVisualPresetRecord : AVisualPresetRecord<ColorVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        [JsonProperty("Color")]
        private Color _Color;

        [JsonConstructor]
        public ColorVisualPresetRecord() { }
        public ColorVisualPresetRecord(ColorVisualID visualID, Color color) : base(visualID) { _Color = color; }

        public Color Color => _Color;

        public override string EditorLabel => "Color";

        
        
        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is ColorVisualPresetRecord colorRecord && Color.Equals(colorRecord.Color);
        }

        public override int GetHashCodeInternal()
        {
            return Color.GetHashCode();
        }
    }
}
