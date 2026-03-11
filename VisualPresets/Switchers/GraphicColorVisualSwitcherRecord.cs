// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class GraphicColorVisualSwitcherRecord : AVisualSwitcherRecord<ColorVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Graphic _Graphic;
        public Graphic Graphic => _Graphic;
        
        public override string EditorLabel => "Graphic Color";
        public override Type EditorIconTargetType => typeof(RawImage);
    }
    
    [Preserve]
    public class GraphicColorTextVisualSwitcherStrategy : AVisualSwitcherStrategy<ColorVisualID, ColorVisualPresetRecord, GraphicColorVisualSwitcherRecord>
    {
        protected override IDisposable Apply(ColorVisualPresetRecord presetRecord, GraphicColorVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Graphic == null)
                return DisposableUtils.Empty;
            
            switcherRecord.Graphic.color = presetRecord.Color;
            return DisposableUtils.Empty;
        }
    }
}
