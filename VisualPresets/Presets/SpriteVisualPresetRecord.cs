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
