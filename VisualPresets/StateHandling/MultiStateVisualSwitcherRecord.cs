// Author: František Holubec
// Created: 17.02.2026

using System;
using System.Collections;
using System.Linq;
using EDIVE.StateHandling.MultiStates;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.StateHandling
{
    [Serializable]
    public class MultiStateVisualSwitcherRecord : AVisualSwitcherRecord<MultiStateVisualID>
    {
        [VerticalGroup("Value")]
        [ValidateMultiState("GetValidStates")]
        [SerializeField]
        private AMultiState _MultiState;
        
        public AMultiState MultiState => _MultiState;
        
        public override string EditorLabel => "MultiState";
        public override Type EditorIconTargetType => typeof(MultiState);

#if UNITY_EDITOR
        private IEnumerable GetValidStates() => VisualID != null ? VisualID.AvailableStates : null;
#endif
    }
    
    [Preserve]
    public class MultiStateVisualSwitcherStrategy : AVisualSwitcherStrategy<MultiStateVisualID, MultiStateVisualPresetRecord, MultiStateVisualSwitcherRecord>
    {
        protected override void Apply(MultiStateVisualPresetRecord presetRecord, MultiStateVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.MultiState == null || string.IsNullOrWhiteSpace(presetRecord.State)) 
                return;
            
            switcherRecord.MultiState.SetState(presetRecord.State);
        }
    }
}
