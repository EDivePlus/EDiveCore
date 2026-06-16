// Author: Michal Petr
// Created: 16.03.2026

using System;
using Cysharp.Threading.Tasks;

namespace EDIVE.ServiceHub.SaveData
{
    [Serializable]
    public abstract class ASaveDataStore
    {
        public SaveDataService SaveDataService { get; set; }
        public SaveDataDomain Context { get; set; }

        public virtual bool SupportsCheapTimestamp => false;

        public virtual void Initialize(SaveDataService saveDataService, SaveDataDomain domain)
        {
            SaveDataService = saveDataService;
            Context = domain;
        }

        public abstract UniTask<StoreReadResult> GetAsync(string key);
        public abstract UniTask SetAsync(ASaveDataObject saveDataObject);
        public abstract UniTask DeleteAsync(string key);
        public abstract UniTask<bool> HasKeyAsync(string key);
        public abstract UniTask FlushAsync();

        public virtual async UniTask<TimestampPeek> GetTimestampAsync(string key)
        {
            var result = await GetAsync(key);
            return result.Found ? TimestampPeek.At(result.Timestamp) : TimestampPeek.Missing;
        }

        public virtual UniTask SetRawAsync(string key, string json, DateTime? timestamp) => UniTask.CompletedTask;
        public virtual UniTask SetTimestampAsync(string key, DateTime timestamp) => UniTask.CompletedTask;
    }
}
