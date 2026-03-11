// Author: František Holubec
// Created: 11.11.2025

using System;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class SpriteRendererColorVisualSwitcherRecord : AVisualSwitcherRecord<ColorVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private SpriteRenderer _SpriteRenderer;
        public SpriteRenderer SpriteRenderer => _SpriteRenderer;
        
        public override string EditorLabel => "SpriteRenderer Color";
        public override Type EditorIconTargetType => typeof(SpriteRenderer);
    }
    
    [Preserve]
    public class SpriteRendererColorTextVisualSwitcherStrategy : AVisualSwitcherStrategy<ColorVisualID, ColorVisualPresetRecord, SpriteRendererColorVisualSwitcherRecord>
    {
        protected override IDisposable Apply(ColorVisualPresetRecord presetRecord, SpriteRendererColorVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.SpriteRenderer == null)
                return DisposableUtils.Empty;
            
            switcherRecord.SpriteRenderer.color = presetRecord.Color;
            return DisposableUtils.Empty;
        }
    }
}
