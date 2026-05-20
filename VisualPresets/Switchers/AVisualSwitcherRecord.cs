// Author: František Holubec
// Created: 10.11.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public abstract class AVisualSwitcherRecord
    {
        public abstract string EditorLabel { get; }
        public abstract Type EditorIconTargetType { get; }
        
        public abstract ABaseVisualID BaseVisualID { get;}
        public virtual bool IsValid() => BaseVisualID != null;

        public abstract void TryApply(AVisualPresetRecord preset);
        
        private static List<IVisualSwitcherStrategy> _strategyCatalog;
        protected static List<IVisualSwitcherStrategy> StrategyCatalog
        {
            get
            {
                if (_strategyCatalog != null)
                    return _strategyCatalog;

                _strategyCatalog = ReflectionExtensions.GetAssignableClassesOfType<IVisualSwitcherStrategy>().ToList();
                Debug.Log($"[VisualSwitcherRecord] Built StrategyCatalog. Count={_strategyCatalog.Count}");
                return _strategyCatalog;
            }
        }
    }
    
    [Serializable]
    public abstract class AVisualSwitcherRecord<TVisualID> : AVisualSwitcherRecord where TVisualID : ABaseVisualID
    {
        [HideLabel]
        [SerializeField]
        protected TVisualID _VisualID;
        
        public TVisualID VisualID => _VisualID;
        public override ABaseVisualID BaseVisualID => _VisualID;
        
        // ReSharper disable StaticFieldInGenericType
        private static List<IVisualSwitcherStrategy<TVisualID>> _typedStrategyCatalog;
        private bool _isInitialized;
        
        private AVisualPresetRecord _currentPreset;
        private IDisposable _strategyHandle;
        
        public override void TryApply(AVisualPresetRecord preset)
        {
            if (_VisualID == null) return;
            if (!Equals(BaseVisualID.UniqueID, preset.BaseVisualID.UniqueID)) return;
            if (Equals(_currentPreset, preset)) return;
            if (preset is not AVisualPresetRecord<TVisualID> tPreset) return;
            TryApply(tPreset);
        }

        private void OnBeforeApply() => _strategyHandle?.Dispose();
        
        public void TryApply(AVisualPresetRecord<TVisualID> presetRecord)
        {
            _typedStrategyCatalog ??= StrategyCatalog.OfType<IVisualSwitcherStrategy<TVisualID>>().ToList();
            if (!_isInitialized)
            {
                _isInitialized = true;
                _typedStrategyCatalog.ForEach(s => s.Prepare(this));
            }
            
            foreach (var strategy in _typedStrategyCatalog)
            {
                if (!strategy.TryApply(out var handle, presetRecord, this, OnBeforeApply)) 
                    continue;
                
                _strategyHandle = handle;
                _currentPreset = presetRecord;
                return;
            }

            Debug.LogWarning($"[VisualSwitcher] No strategy found for preset {presetRecord.VisualID.name}");
            _strategyHandle?.Dispose();
            _currentPreset = null;
        }
    }
}
