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
        protected static List<IVisualSwitcherStrategy> StrategyCatalog => _strategyCatalog ??= ReflectionExtensions.GetAssignableClassesOfType<IVisualSwitcherStrategy>().ToList();
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
        private IVisualSwitcherStrategy<TVisualID> _currentStrategy;
        private bool _isInitialized;
        
        public override void TryApply(AVisualPresetRecord preset)
        {
            if (preset is AVisualPresetRecord<TVisualID> tPreset)
                TryApply(tPreset);
        }

        public void TryApply(AVisualPresetRecord<TVisualID> presetRecord)
        {
            _typedStrategyCatalog ??= StrategyCatalog.OfType<IVisualSwitcherStrategy<TVisualID>>().ToList();
            if (!_isInitialized)
            {
                _isInitialized = true;
                _typedStrategyCatalog.ForEach(s => s.CleanUp(this));
            }
            
            foreach (var strategy in _typedStrategyCatalog)
            {
                if (!strategy.TryApply(presetRecord, this, OnBeforeApply)) 
                    continue;
                
                _currentStrategy = strategy;
                return;

                void OnBeforeApply()
                {
                    if (_currentStrategy != strategy && _currentStrategy != null) 
                        _currentStrategy.CleanUp(this);
                }
            }
            _currentStrategy = null;
        }
    }
}
