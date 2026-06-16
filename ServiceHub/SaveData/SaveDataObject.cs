// Author: Michal Petr
// Created: 18.03.2026

using System;

namespace EDIVE.ServiceHub.SaveData
{
    [Flags]
    public enum SaveDataDirtyFlag
    {
        None = 0,                // No changes to be made
        OnBatch = 1 << 0,        // Synced with remote once SaveDataService calls BatchSync (can be triggered manually or automatically at certain intervals/events)
        OnEndOfFrame = 1 << 1,   // Synced with remote at the end of the frame (after all changes for the frame are done)
        Immediate = 1 << 2,      // Synced with remote immediately whenever a change is made
    }
    
    public abstract class ASaveDataObject
    {
        public abstract string Key { get; }
        
        public SaveDataDirtyFlag DirtyFlags { get; private set; } = SaveDataDirtyFlag.None;
        public bool IsDirty => DirtyFlags != SaveDataDirtyFlag.None;

        protected virtual SaveDataDirtyFlag DefaultDirtyFlags => SaveDataDirtyFlag.OnBatch;
        
        public event Action<SaveDataDirtyFlag> DirtyFlagsChanged;
        public event Action MarkedAsDirty;
        public event Action MarkedAsClean;

        protected void SetProperty<T>(ref T field, T value, SaveDataDirtyFlag? dirtyFlag = null)
        {
            var effectiveFlag = dirtyFlag ?? DefaultDirtyFlags;
            if (Equals(field, value)) return;
            field = value;
            SetDirty(effectiveFlag);
        }
        
        public void SetDirty(SaveDataDirtyFlag? flag = null)
        {
            var effectiveFlag = flag ?? DefaultDirtyFlags;

            var wasClean = DirtyFlags == SaveDataDirtyFlag.None;
            DirtyFlags |= effectiveFlag;

            if (wasClean)
                MarkedAsDirty?.Invoke();
            DirtyFlagsChanged?.Invoke(DirtyFlags);
        }
        
        public void ClearDirty()
        {
            if (DirtyFlags == SaveDataDirtyFlag.None) 
                return;
            
            DirtyFlags = SaveDataDirtyFlag.None;
            DirtyFlagsChanged?.Invoke(DirtyFlags);
            MarkedAsClean?.Invoke();
        }
    }
}
