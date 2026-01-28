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
    public class TMPTextStringVisualSwitcherRecord : AVisualSwitcherRecord<StringVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private TMP_Text _Text;
        public TMP_Text Text => _Text;
        
        public override string EditorLabel => "TMP Text";
        public override Type EditorIconTargetType => typeof(TextMeshProUGUI);
    }
    
    [Preserve]
    public class TMPTextStringVisualSwitcherStrategy : AVisualSwitcherStrategy<StringVisualID, StringVisualPresetRecord, TMPTextStringVisualSwitcherRecord>
    {
        protected override void Apply(StringVisualPresetRecord presetRecord, TMPTextStringVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null) 
                return;
            
            switcherRecord.Text.text = presetRecord.Text;
        }
    }
}
