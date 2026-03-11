// Author: František Holubec
// Created: 11.11.2025

using System;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class ImageSpriteVisualSwitcherRecord : AVisualSwitcherRecord<SpriteVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Image _Image;
        public Image Image => _Image;
        
        public override string EditorLabel => "Image Sprite";
        public override Type EditorIconTargetType => typeof(Image);
    }
    
    [Preserve]
    public class ImageSpriteTextVisualSwitcherStrategy : AVisualSwitcherStrategy<SpriteVisualID, SpriteVisualPresetRecord, ImageSpriteVisualSwitcherRecord>
    {
        protected override IDisposable Apply(SpriteVisualPresetRecord presetRecord, ImageSpriteVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Image == null)
                return DisposableUtils.Empty;
            
            switcherRecord.Image.sprite = presetRecord.Sprite;
            return DisposableUtils.Empty;
        }
    }
}
