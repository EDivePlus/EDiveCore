// Author: Michal Petr
// Created: 18.06.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.ServiceHub.Auth;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData
{
    [Serializable]
    public class SaveDataDomain
    {
        [SerializeField]
        private string _Key;
        public string Key => _Key;

        [SerializeReference]
        private ISaveDataConflictResolver _ConflictResolver;

        [SerializeReference]
        [PropertyOrder(10)]
        private List<ASaveDataStore> _Stores = new();
        public List<ASaveDataStore> Stores => _Stores;
        
        public AuthStorage Auth { get; internal set; }
        
        private Dictionary<string, ASaveDataObject> _objectCache;
        private Dictionary<string, ASaveDataObject> ObjectCache => _objectCache ??= new Dictionary<string, ASaveDataObject>(StringComparer.Ordinal);
        
        private ServiceHubSettings _settings;

        public void Initialize(SaveDataService saveDataService, AuthStorage auth)
        {
            _settings = saveDataService.Settings;
            Auth = auth;
            _Stores.ForEach(store => store.Initialize(saveDataService, this));
        }

        public bool TryGetCached<T>(string key, out T value) where T : ASaveDataObject
        {
            if (ObjectCache.TryGetValue(key, out var cached) && cached is T typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }
        
        public T ResolveCacheFromJson<T>(string key, string json) where T : ASaveDataObject
        {
            if (ObjectCache.TryGetValue(key, out var cached) && cached is T typed)
            {
                JsonConvert.PopulateObject(json, typed);
                return typed;
            }

            var created = JsonConvert.DeserializeObject<T>(json);
            if (created != null)
                ObjectCache[key] = created;

            return created;
        }

        public void CacheObject(ASaveDataObject saveDataObject)
        {
            if (saveDataObject != null)
                ObjectCache[saveDataObject.Key] = saveDataObject;
        }

        public void RemoveCached(string key) => ObjectCache.Remove(key);
        public void ClearCache() => ObjectCache.Clear();

        private static readonly ISaveDataConflictResolver DEFAULT_RESOLVER = new NewestWinsConflictResolver();
        private ISaveDataConflictResolver Resolver => _ConflictResolver ?? DEFAULT_RESOLVER;

        public async UniTask<SaveDataResult<T>> GetSaveDataAsync<T>(string key, CancellationToken ct = default) where T : ASaveDataObject
        {
            if (TryGetCached<T>(key, out var cached))
                return SaveDataResult<T>.Success(cached, false);

            var count = _Stores.Count;
            var peeks = new TimestampPeek[count];
            var fullReads = new StoreReadResult?[count];
            var candidates = new List<SaveDataCandidate>(count);

            for (var i = 0; i < count; i++)
            {
                var store = _Stores[i];
                if (store == null)
                    continue;

                if (store.SupportsCheapTimestamp)
                {
                    peeks[i] = await store.GetTimestampAsync(key);
                }
                else
                {
                    var read = await store.GetAsync(key);
                    fullReads[i] = read;
                    peeks[i] = read.Found ? TimestampPeek.At(read.Timestamp) : TimestampPeek.Missing;
                }

                if (peeks[i].Found)
                    candidates.Add(new SaveDataCandidate(i, peeks[i].Timestamp));
            }

            if (candidates.Count == 0)
                return SaveDataResult<T>.Success(null, false);

            var winnerIndex = Resolver.Resolve(candidates);
            if (winnerIndex < 0 || winnerIndex >= count || _Stores[winnerIndex] == null)
                return SaveDataResult<T>.Success(null, false);

            var winnerRead = fullReads[winnerIndex] ?? await _Stores[winnerIndex].GetAsync(key);
            if (!winnerRead.Found)
                return SaveDataResult<T>.Success(null, false);

            var value = ResolveCacheFromJson<T>(key, winnerRead.Json);

            for (var i = 0; i < count; i++)
            {
                if (i == winnerIndex || _Stores[i] == null)
                    continue;
                if (peeks[i].Found && TimestampsEqual(peeks[i].Timestamp, winnerRead.Timestamp))
                    continue;
                await _Stores[i].SetRawAsync(key, winnerRead.Json, winnerRead.Timestamp);
            }

            return SaveDataResult<T>.Success(value, winnerIndex > 0);
        }

        private static bool TimestampsEqual(DateTime? a, DateTime? b) =>
            a.HasValue && b.HasValue ? a.Value == b.Value : a.HasValue == b.HasValue;

        public async UniTask SetSaveDataAsync(ASaveDataObject saveDataObject, SaveDataDirtyFlag flag)
        {
            saveDataObject.SetDirty(flag);
            CacheObject(saveDataObject);

            foreach (var store in _Stores)
            {
                if (store != null)
                    await store.SetAsync(saveDataObject);
            }
        }

        public async UniTask FlushAsync()
        {
            foreach (var store in _Stores)
            {
                if (store != null)
                    await store.FlushAsync();
            }
        }

        public void OnRemoteWriteConfirmed(string key, DateTime? timestamp)
        {
            if (timestamp == null)
                return;

            foreach (var store in _Stores)
                store?.SetTimestampAsync(key, timestamp.Value).Forget();
        }
    }
}
