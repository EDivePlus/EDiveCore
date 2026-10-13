// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
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
                
        // Target values from before the first apply
        private VisualPreset _defaults;

        public void Apply(AVisualPresetRecord record)
        {
            if (record == null) 
                return;
            
            _defaults ??= Capture();
            foreach (var switcher in _Records)
            {
                switcher?.TryApply(record);
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
            if (presets == null) return;
            foreach (var bundle in presets)
            {
                if (bundle == null) continue;
                Apply(bundle);
            }
        }

        // Like Apply, IDs the preset leaves out go back to their value from before the first apply, e.g. for reused views
        public void ApplyExclusive(VisualPreset preset)
        {
            _defaults ??= Capture();
            foreach (var record in _defaults.EnumerateValidRecords())
            {
                if (!Contains(preset, record.BaseVisualID))
                    Apply(record);
            }
            Apply(preset);
        }

        // Current target values
        public VisualPreset Capture()
        {
            return new VisualPreset(_Records.Where(r => r != null).Select(r => r.Capture()).Where(r => r != null));
        }

        private static bool Contains(VisualPreset preset, ABaseVisualID visualID)
        {
            if (preset == null || visualID == null)
                return false;

            foreach (var record in preset.EnumerateValidRecords())
            {
                if (Equals(record.BaseVisualID.UniqueID, visualID.UniqueID))
                    return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private IEnumerable GetAvailableRecords()
        {
            return TypeCacheUtils.GetDerivedClassesOfType<AVisualSwitcherRecord>()
                .Select(r => new ValueDropdownItem<AVisualSwitcherRecord>(r.EditorLabel, r));
        }
        
        private Texture2D GetRecordIcon(AVisualSwitcherRecord record) => GUIHelper.GetAssetThumbnail(null, record.EditorIconTargetType, false);
        
        public IEnumerable<UnityEngine.Object> GetTargets()
        {
            return _Records.Where(r => r != null && r.Target != null).Select(r => r.Target).Distinct();
        }

        public IEnumerable<ABaseVisualID> GetVisualIDs()
        {
            return _Records.Select(r => r.BaseVisualID).Where(id => id != null);
        }
#endif
    }
}
