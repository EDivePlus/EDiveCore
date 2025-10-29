// Author: Michal Petr
// Created: 29.10.2025

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.VisualPresets.Presets
{
    [HideLabel]
    [InlineProperty]
    [Serializable]
    public class VisualPreset
    {
        [SerializeReference]
        [EnhancedTableList]
        [HideReferenceObjectPicker]
        [LabelText("@$property.Parent.NiceName")]
        [EnhancedValueDropdown("GetAvailableRecords", DrawDropdownForListElements = false)]
        private List<AVisualPresetRecord> _Records = new();

        public VisualPreset() { }
        public VisualPreset(params AVisualPresetRecord[] records)
        {
            AddRecords(records);
        }

        public virtual IEnumerable<AVisualPresetRecord> EnumerateValidRecords()
        {
            return _Records.Where(r => r != null && r.IsValid());
        }
        
        public void AddRecords(params AVisualPresetRecord[] records)
        {
            if (records == null || records.Length == 0)
                return;

            _Records ??= new List<AVisualPresetRecord>();
            foreach (var record in records)
            {
                if (record == null)
                    continue;

                _Records.Add(record);
            }
        }
        
        public bool TryGetRecord(ABaseVisualID visualID, out AVisualPresetRecord record)
        {
            return _Records.TryGetFirst(r => r.BaseVisualID == visualID, out record);
        }

#if UNITY_EDITOR
        private IEnumerable GetAvailableRecords()
        {
            return TypeCacheUtils.GetDerivedClassesOfType<AVisualPresetRecord>();
        }
#endif
    }
}
