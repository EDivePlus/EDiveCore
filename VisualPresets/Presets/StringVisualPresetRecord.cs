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
    [JsonTypeName("VisualPreset.String")]
    public class StringVisualPresetRecord : AVisualPresetRecord<StringVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        [JsonProperty("Text")]
        private string _Text;
        
        public string Text => _Text;

        public override string EditorLabel => "String";

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is StringVisualPresetRecord tOther && Text == tOther.Text;
        }

        public override int GetHashCodeInternal()
        {
            return Text?.GetHashCode() ?? 0;
        }
    }
}
