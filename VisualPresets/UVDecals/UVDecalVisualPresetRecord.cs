// Author: Michal Petr
// Created: 21.09.2026

using System;
using EDIVE.Rendering.UVDecals;
using EDIVE.Utils.Json.TypeNames;
using EDIVE.VisualPresets.Presets;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.UVDecals
{

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [JsonTypeName("VisualPreset.UVDecal")]
    public class UVDecalVisualPresetRecord : AVisualPresetRecord<UVDecalVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        [HideLabel]
        [JsonProperty("Decal")]
#if PURRNET
        [JsonConverter(typeof(UVDecalPresetJsonConverter))]
#endif
        private UVDecalPreset _Decal;

        public UVDecalPreset Decal => _Decal;

        public override string EditorLabel => "UV Decal";

        [JsonConstructor]
        public UVDecalVisualPresetRecord() { }
        public UVDecalVisualPresetRecord(UVDecalVisualID visualID, UVDecalPreset decal) : base(visualID) { _Decal = decal; }

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is UVDecalVisualPresetRecord decalRecord && Equals(Decal, decalRecord.Decal);
        }

        public override int GetHashCodeInternal()
        {
            return Decal?.GetHashCode() ?? 0;
        }
    }
}
