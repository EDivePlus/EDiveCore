// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;
using UnityEngine;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class SpriteRendererSpriteVisualSwitcherRecord : AVisualSwitcherRecord<SpriteVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private SpriteRenderer _SpriteRenderer;
        public SpriteRenderer SpriteRenderer => _SpriteRenderer;
    }
    
    [Preserve]
    public class SpriteRendererSpriteTextVisualSwitcherStrategy : AVisualSwitcherStrategy<SpriteVisualID, SpriteVisualPresetRecord, SpriteRendererSpriteVisualSwitcherRecord>
    {
        protected override void Apply(SpriteVisualPresetRecord presetRecord, SpriteRendererSpriteVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.SpriteRenderer == null) 
                return;
            
            switcherRecord.SpriteRenderer.sprite = presetRecord.Sprite;
        }
    }
}
