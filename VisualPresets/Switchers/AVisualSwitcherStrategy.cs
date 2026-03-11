// Author: František Holubec
// Created: 10.11.2025

using System;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;

namespace EDIVE.VisualPresets.Switchers
{
    public interface IVisualSwitcherStrategy
    {
        bool TryApply(out IDisposable handle, AVisualPresetRecord presetRecord, AVisualSwitcherRecord switcherRecord, Action onBeforeApply = null);
        void Prepare(AVisualSwitcherRecord switcherRecord);
    }
    
    public interface IVisualSwitcherStrategy<TVisualID> : IVisualSwitcherStrategy where TVisualID : ABaseVisualID
    {

    }
    
    public abstract class AVisualSwitcherStrategy<TVisualID, TPresetRecord, TSwitcherRecord> : IVisualSwitcherStrategy<TVisualID>
        where TVisualID : ABaseVisualID
        where TPresetRecord : AVisualPresetRecord<TVisualID>
        where TSwitcherRecord : AVisualSwitcherRecord<TVisualID>
    {
        public bool TryApply(out IDisposable handle, AVisualPresetRecord presetRecord, AVisualSwitcherRecord switcherRecord, Action onBeforeApply = null)
        {
            handle = null;
            if (presetRecord is not TPresetRecord tPresetRecord || switcherRecord is not TSwitcherRecord tSwitcherRecord)
                return false;
            
            onBeforeApply?.Invoke();
            handle = Apply(tPresetRecord, tSwitcherRecord);
            return true;
        }
        
        protected abstract IDisposable Apply(TPresetRecord presetRecord, TSwitcherRecord switcherRecord);
        
        public virtual void Prepare(AVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord is TSwitcherRecord tSwitcherRecord)
                Prepare(tSwitcherRecord);
        }
        
        protected virtual void Prepare(TSwitcherRecord switcherRecord) { }
    }
}
