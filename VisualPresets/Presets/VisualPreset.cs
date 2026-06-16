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
using Newtonsoft.Json;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.VisualPresets.Presets
{
    [HideLabel]
    [InlineProperty]
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class VisualPreset
    {
        [SerializeReference]
        [EnhancedTableList]
        [HideReferenceObjectPicker]
        [LabelText("@$property.Parent.NiceName")]
        [EnhancedValueDropdown("GetAvailableRecords", DrawDropdownForListElements = false, SortDropdownItems = true)]
        [JsonProperty("Records", ItemTypeNameHandling = TypeNameHandling.Auto, ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private List<AVisualPresetRecord> _Records = new();

        public VisualPreset() { }
        public VisualPreset(params AVisualPresetRecord[] records)
        {
            AddRecords(records);
        }
        public VisualPreset(IEnumerable<AVisualPresetRecord> records)
        {
            AddRecords(records);
        }

        public virtual IEnumerable<AVisualPresetRecord> EnumerateValidRecords()
        {
            return _Records.Where(r => r != null && r.IsValid());
        }
        
        public void AddRecords(IEnumerable<AVisualPresetRecord> records)
        {
            if (records == null)
                return;

            _Records ??= new List<AVisualPresetRecord>();
            foreach (var record in records)
            {
                if (record == null)
                    continue;

                _Records.Add(record);
            }
        }
        
        public bool TryGetRecord<TVisualID, TRecord>(TVisualID visualID, out TRecord record) 
            where TVisualID : ABaseVisualID 
            where TRecord : AVisualPresetRecord<TVisualID>
        {
            if (_Records.TryGetFirst(r => r.BaseVisualID == visualID, out var baseRecord) && baseRecord is TRecord typedRecord)
            {
                record = typedRecord;
                return true;
            }

            record = null;
            return false;
        }
        
        public VisualPreset CombineWith(VisualPreset other)
        {
            return other == null ? this : new VisualPreset(_Records.Concat(other._Records));
        }
        
        public static VisualPreset Combine(params VisualPreset[] presets)
        {
            return new VisualPreset(presets.Where(p => p != null).SelectMany(p => p._Records));
        }

#if UNITY_EDITOR
        private IEnumerable GetAvailableRecords()
        {
            return TypeCacheUtils.GetDerivedClassesOfType<AVisualPresetRecord>()
                .Select(r => new ValueDropdownItem<AVisualPresetRecord>(r.EditorLabel, r));
        }
#endif
    }
    
    
}
