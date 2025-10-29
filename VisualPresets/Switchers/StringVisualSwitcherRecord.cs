// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class StringVisualSwitcherRecord : AVisualSwitcherRecord<StringVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private TMP_Text _Text;
        public TMP_Text Text => _Text;
    }
    
    [Preserve]
    public class StringTextVisualSwitcherStrategy : AVisualSwitcherStrategy<StringVisualID, StringVisualPresetRecord, StringVisualSwitcherRecord>
    {
        protected override void Apply(StringVisualPresetRecord presetRecord, StringVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null) 
                return;
            
            switcherRecord.Text.text = presetRecord.Text;
        }
    }
}
