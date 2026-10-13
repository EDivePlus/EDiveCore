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

        // Object the record changes, marked dirty after edit mode applies
        public virtual UnityEngine.Object Target => null;
        public virtual bool IsValid() => BaseVisualID != null;

        public abstract void TryApply(AVisualPresetRecord preset);

        // Current target value as a preset record, null when no strategy supports it
        public abstract AVisualPresetRecord Capture();
        
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
        
        public sealed override void TryApply(AVisualPresetRecord preset)
        {
            if (_VisualID == null || preset?.BaseVisualID == null) return;
            if (!Equals(BaseVisualID.UniqueID, preset.BaseVisualID.UniqueID)) return;
            if (Equals(_currentPreset, preset)) return;
            if (preset is not AVisualPresetRecord<TVisualID> tPreset) return;
            TryApply(tPreset);
        }

        private void OnBeforeApply() => _strategyHandle?.Dispose();
        
        private static List<IVisualSwitcherStrategy<TVisualID>> TypedStrategyCatalog
        {
            get
            {
                if (_typedStrategyCatalog != null)
                    return _typedStrategyCatalog;

                _typedStrategyCatalog = StrategyCatalog.OfType<IVisualSwitcherStrategy<TVisualID>>().ToList();
                return _typedStrategyCatalog;
            }
        }

        public void TryApply(AVisualPresetRecord<TVisualID> presetRecord)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                TypedStrategyCatalog.ForEach(s => s.Prepare(this));
            }
            
            foreach (var strategy in TypedStrategyCatalog)
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

        public sealed override AVisualPresetRecord Capture()
        {
            if (_VisualID == null)
                return null;

            foreach (var strategy in TypedStrategyCatalog)
            {
                var presetRecord = strategy.Capture(this);
                if (presetRecord != null)
                    return presetRecord;
            }
            return null;
        }
    }
}
