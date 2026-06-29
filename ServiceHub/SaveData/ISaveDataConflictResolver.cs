// Author: Michal Petr
// Created: 25.06.2026

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.ServiceHub.SaveData
{
    public readonly struct SaveDataCandidate
    {
        public int StoreIndex { get; }
        public DateTime? Timestamp { get; }

        public SaveDataCandidate(int storeIndex, DateTime? timestamp)
        {
            StoreIndex = storeIndex;
            Timestamp = timestamp;
        }
    }
    
    [EnhancedTypeSelector(true, 1)]
    public interface ISaveDataConflictResolver
    {
        int Resolve(IReadOnlyList<SaveDataCandidate> candidates);
    }
}
