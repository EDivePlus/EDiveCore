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
    // Note: the raw GameObject reference is intentionally not serialized to JSON (it cannot round-trip);
    // only the VisualID (ID) and the type discriminator are persisted.
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [JsonTypeName("VisualPreset.Prefab")]
    public class PrefabVisualPresetRecord : AVisualPresetRecord<PrefabVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private GameObject _Prefab;
        
        public GameObject Prefab => _Prefab;

        public override string EditorLabel => "Prefab";

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is PrefabVisualPresetRecord prefabRecord && _Prefab == prefabRecord._Prefab;
        }

        public override int GetHashCodeInternal()
        {
            return Prefab != null ? Prefab.GetHashCode() : 0;
        }
    }
}
