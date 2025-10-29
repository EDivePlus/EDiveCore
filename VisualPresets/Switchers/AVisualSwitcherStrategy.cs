// Author: František Holubec
// Created: 10.11.2025

using System;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;

namespace EDIVE.VisualPresets.Switchers
{
    public interface IVisualSwitcherStrategy
    {
        bool TryApply(AVisualPresetRecord presetRecord, AVisualSwitcherRecord switcherRecord, Action onBeforeApply = null);
        void CleanUp(AVisualSwitcherRecord switcherRecord);
    }
    
    public interface IVisualSwitcherStrategy<TVisualID> : IVisualSwitcherStrategy where TVisualID : ABaseVisualID
    {

    }
    
    public abstract class AVisualSwitcherStrategy<TVisualID, TPresetRecord, TSwitcherRecord> : IVisualSwitcherStrategy<TVisualID>
        where TVisualID : ABaseVisualID
        where TPresetRecord : AVisualPresetRecord<TVisualID>
        where TSwitcherRecord : AVisualSwitcherRecord<TVisualID>
    {
        public bool TryApply(AVisualPresetRecord presetRecord, AVisualSwitcherRecord switcherRecord, Action onBeforeApply = null)
        {
            if (presetRecord is not TPresetRecord tPresetRecord || switcherRecord is not TSwitcherRecord tSwitcherRecord)
                return false;
            
            onBeforeApply?.Invoke();
            Apply(tPresetRecord, tSwitcherRecord);
            return true;
        }
        
        protected abstract void Apply(TPresetRecord presetRecord, TSwitcherRecord switcherRecord);

        public void CleanUp(AVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord is TSwitcherRecord tSwitcherRecord) 
                CleanUp(tSwitcherRecord);
        }

        protected virtual void CleanUp(TSwitcherRecord switcherRecord) { }
    }
}
