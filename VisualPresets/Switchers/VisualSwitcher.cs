// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.VisualPresets.Switchers
{
    [HideLabel]
    [InlineProperty]
    [Serializable]
    public class VisualSwitcher
    {
        [SerializeReference]
        [EnhancedTableList]
        [HideReferenceObjectPicker]
        [LabelText("@$property.Parent.NiceName")]
        [EnhancedValueDropdown("GetAvailableRecords", DrawDropdownForListElements = false, IconGetter = "GetRecordIcon", SortDropdownItems = true)]
        private List<AVisualSwitcherRecord> _Records = new();
                
        public void Apply(AVisualPresetRecord record)
        {
            if (record == null) 
                return;
            
            foreach (var switcher in _Records)
            {
                if (switcher != null && switcher.BaseVisualID == record.BaseVisualID)
                    switcher.TryApply(record);
            }
        }
        
        public void Apply(IEnumerable<AVisualPresetRecord> records)
        {
            if (records == null) return;
            foreach (var preset in records)
            {
                Apply(preset);
            }
        }

        public void Apply(VisualPreset preset)
        {
            if (preset == null) return;
            Apply(preset.EnumerateValidRecords());
        }
        
        public void Apply(IEnumerable<VisualPreset> presets)
        {
            foreach (var bundle in presets)
            {
                if (bundle == null) return;
                Apply(bundle);
            }
        }

#if UNITY_EDITOR
        private IEnumerable GetAvailableRecords()
        {
            return TypeCacheUtils.GetDerivedClassesOfType<AVisualSwitcherRecord>()
                .Select(r => new ValueDropdownItem<AVisualSwitcherRecord>(r.EditorLabel, r));
        }
        
        private Texture2D GetRecordIcon(AVisualSwitcherRecord record) => GUIHelper.GetAssetThumbnail(null, record.EditorIconTargetType, false);
#endif
    }
}
