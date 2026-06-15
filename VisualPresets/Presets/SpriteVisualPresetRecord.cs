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
    // Note: the raw Sprite reference is intentionally not serialized to JSON (it cannot round-trip);
    // only the VisualID (ID) and the type discriminator are persisted.
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [JsonTypeName("VisualPreset.Sprite")]
    public class SpriteVisualPresetRecord : AVisualPresetRecord<SpriteVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Sprite _Sprite;
        
        public Sprite Sprite => _Sprite;

        public override string EditorLabel => "Sprite";

        [JsonConstructor]
        public SpriteVisualPresetRecord() { }
        public SpriteVisualPresetRecord(SpriteVisualID visualID, Sprite sprite) : base(visualID) { _Sprite = sprite; }

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is SpriteVisualPresetRecord spriteRecord && Sprite == spriteRecord.Sprite;
        }

        public override int GetHashCodeInternal()
        {
            return Sprite != null ? Sprite.GetHashCode() : 0;
        }
    }
}
