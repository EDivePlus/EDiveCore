// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.NativeUtils;
using EDIVE.Utils.FontSymbols;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class FontSymbolVisualPresetRecord : AVisualPresetRecord<FontSymbolVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private FontSymbol _FontSymbol;
        
        public FontSymbol FontSymbol
        {
            get => _FontSymbol;
            set => _FontSymbol = value;
        }
        
        public override string EditorLabel => "Font Symbol";
    }
    
    [Serializable]
    public class TextFontSymbolVisualSwitcherRecord : AVisualSwitcherRecord<FontSymbolVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private FontSymbolText _Text;
        public FontSymbolText Text => _Text;
        
        public override string EditorLabel => "Text FontSymbol";
        public override Type EditorIconTargetType => typeof(FontSymbolText);
    }
    
    [Preserve]
    public class TextFontSymbolVisualSwitcherStrategy : AVisualSwitcherStrategy<FontSymbolVisualID, FontSymbolVisualPresetRecord, TextFontSymbolVisualSwitcherRecord>
    {
        protected override IDisposable Apply(FontSymbolVisualPresetRecord presetRecord, TextFontSymbolVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null) 
                return DisposableUtils.Empty;
            
            switcherRecord.Text.FontSymbol = presetRecord.FontSymbol;
            return DisposableUtils.Empty;
        }
    }
    
    [Serializable]
    public class TMPTextFontSymbolVisualSwitcherRecord : AVisualSwitcherRecord<FontSymbolVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private FontSymbolTMPTextUI _Text;
        public FontSymbolTMPTextUI Text => _Text;
        
        public override string EditorLabel => "TMP FontSymbol";
        public override Type EditorIconTargetType => typeof(FontSymbolTMPTextUI);
    }
    
    [Preserve]
    public class TMPTextFontSymbolVisualSwitcherStrategy : AVisualSwitcherStrategy<FontSymbolVisualID, FontSymbolVisualPresetRecord, TMPTextFontSymbolVisualSwitcherRecord>
    {
        protected override IDisposable Apply(FontSymbolVisualPresetRecord presetRecord, TMPTextFontSymbolVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null)
                return DisposableUtils.Empty;
            
            switcherRecord.Text.FontSymbol = presetRecord.FontSymbol;
            return DisposableUtils.Empty;
        }
    }
}
