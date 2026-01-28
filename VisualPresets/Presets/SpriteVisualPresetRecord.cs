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
        
        public Sprite Sprite
        {
            get => _Sprite;
            set => _Sprite = value;
        }
        
        public override string EditorLabel => "Sprite";

    }
}
