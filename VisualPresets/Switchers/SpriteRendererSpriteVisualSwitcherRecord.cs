// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.NativeUtils;
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
        
        public override string EditorLabel => "SpriteRenderer Sprite";
        public override Type EditorIconTargetType => typeof(SpriteRenderer);
    }
    
    [Preserve]
    public class SpriteRendererSpriteTextVisualSwitcherStrategy : AVisualSwitcherStrategy<SpriteVisualID, SpriteVisualPresetRecord, SpriteRendererSpriteVisualSwitcherRecord>
    {
        protected override IDisposable Apply(SpriteVisualPresetRecord presetRecord, SpriteRendererSpriteVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.SpriteRenderer == null) 
                return DisposableUtils.Empty;
            
            switcherRecord.SpriteRenderer.sprite = presetRecord.Sprite;
            return DisposableUtils.Empty;
        }
    }
}
