// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public class SpriteVisualPresetRecord : AVisualPresetRecord<SpriteVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Sprite _Sprite;
        
        public Sprite Sprite => _Sprite;

        public override string EditorLabel => "Sprite";

        public override bool Equals(AVisualPresetRecord other)
        {
            if (other is not SpriteVisualPresetRecord spriteRecord)
                return false;
            return base.Equals(other) && Sprite == spriteRecord.Sprite;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Sprite != null ? Sprite.GetHashCode() : 0);
        }
    }
}
