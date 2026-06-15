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
    // Note: the raw Material reference is intentionally not serialized to JSON (it cannot round-trip);
    // only the VisualID (ID) and the type discriminator are persisted.
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [JsonTypeName("VisualPreset.Material")]
    public class MaterialVisualPresetRecord : AVisualPresetRecord<MaterialVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Material _Material;
        
        public Material Material => _Material;

        public override string EditorLabel => "Material";

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is MaterialVisualPresetRecord materialRecord && Equals(Material, materialRecord.Material);
        }

        public override int GetHashCodeInternal()
        {
            return Material != null ? Material.GetHashCode() : 0;
        }
    }
}
