// Author: Michal Petr
// Created: 18.03.2026

using System;

namespace EDIVE.UserCenter.SaveData
{
    public enum SaveDataDirtyFlag
    {
        NoChange,       // No changes to be made
        Manual,         // Synced with remote only when manually triggered
        OnBatch,        // Synced with remote once UserCenterManager calls BatchSync (can be triggered manually or automatically at certain intervals/events)
        OnEndOfFrame,   // Synced with remote at the end of the frame (after all changes for the frame are done)
        Immediate,      // Synced with remote immediately whenever a change is made
    }
    
    public abstract class ASaveDataObject
    {
        private SaveDataDirtyFlag DirtyFlag { get; set; } = SaveDataDirtyFlag.NoChange;
        public bool IsDirty => DirtyFlag != SaveDataDirtyFlag.NoChange;

        protected virtual SaveDataDirtyFlag DefaultDirtyFlag => SaveDataDirtyFlag.OnBatch;
        
        public event Action<SaveDataDirtyFlag> DirtyFlagChanged;
        public event Action MarkedAsDirty;
        public event Action MarkedAsClean;

        protected void SetProperty<T>(ref T field, T value, SaveDataDirtyFlag? dirtyFlag = null)
        {
            var effectiveFlag = dirtyFlag ?? DefaultDirtyFlag;
            if (Equals(field, value)) return;
            field = value;
            SetDirty(effectiveFlag);
        }
        
        public void SetDirty(SaveDataDirtyFlag? flag = null)
        {
            var effectiveFlag = flag ?? DefaultDirtyFlag;
            if (effectiveFlag <= DirtyFlag) return;

            var wasClean = DirtyFlag == SaveDataDirtyFlag.NoChange;
            DirtyFlag = effectiveFlag;

            if (wasClean)
                MarkedAsDirty?.Invoke();
            DirtyFlagChanged?.Invoke(DirtyFlag);
        }
        
        public void ClearDirty()
        {
            DirtyFlag = SaveDataDirtyFlag.NoChange;
            DirtyFlagChanged?.Invoke(DirtyFlag);
            MarkedAsClean?.Invoke();
        }
    }
}
