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
        private ISaveDataConflictResolver _ConflictResolver = new NewestWinsConflictResolver();

        [SerializeReference]
        [PropertyOrder(10)]
        private List<ASaveDataStore> _Stores = new();
        
        public List<ASaveDataStore> Stores => _Stores;
        
        public AuthStorage Auth { get; private set; }
        
        private Dictionary<string, ASaveDataObject> _objectCache;
        private Dictionary<string, ASaveDataObject> ObjectCache => _objectCache ??= new Dictionary<string, ASaveDataObject>(StringComparer.Ordinal);
        
        private ServiceHubSettings _settings;

        public SaveDataDomain() { }
        public SaveDataDomain(string key, IEnumerable<ASaveDataStore> stores = null, ISaveDataConflictResolver conflictResolver = null)
        {
            _Key = key;
            _Stores = stores != null ? new List<ASaveDataStore>(stores) : new List<ASaveDataStore>();
            _ConflictResolver = conflictResolver ?? new NewestWinsConflictResolver();
        }

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
                CacheObject(created);

            return created;
        }

        public void CacheObject(ASaveDataObject saveDataObject)
        {
            if (saveDataObject == null)
                return;

            var key = saveDataObject.Key;
            if (ObjectCache.TryGetValue(key, out var existing) && existing != saveDataObject)
                Untrack(existing);

            ObjectCache[key] = saveDataObject;
            Track(saveDataObject);
        }

        public void RemoveCached(string key)
        {
            if (ObjectCache.TryGetValue(key, out var cached))
                Untrack(cached);
            ObjectCache.Remove(key);
        }

        public void ClearCache()
        {
            foreach (var cached in ObjectCache.Values)
                Untrack(cached);
            ObjectCache.Clear();
        }

        private void Track(ASaveDataObject saveDataObject)
        {
            saveDataObject.MarkedAsDirty -= OnObjectMarkedDirty;
            saveDataObject.MarkedAsDirty += OnObjectMarkedDirty;
        }

        private void Untrack(ASaveDataObject saveDataObject)
        {
            saveDataObject.MarkedAsDirty -= OnObjectMarkedDirty;
        }

        private void OnObjectMarkedDirty(ASaveDataObject saveDataObject) => PersistAsync(saveDataObject).Forget();

        private static readonly ISaveDataConflictResolver DEFAULT_RESOLVER = new NewestWinsConflictResolver();
        private ISaveDataConflictResolver Resolver => _ConflictResolver ?? DEFAULT_RESOLVER;

        private T GetOrCreateTracked<T>(string key) where T : ASaveDataObject, new()
        {
            if (TryGetCached<T>(key, out var cached))
                return cached;

            var created = new T();
            CacheObject(created);
            return created;
        }

        public async UniTask<SaveDataResult<T>> GetSaveDataAsync<T>(string key, CancellationToken ct = default) where T : ASaveDataObject, new()
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
                return SaveDataResult<T>.Success(GetOrCreateTracked<T>(key), false);

            var winnerIndex = Resolver.Resolve(candidates);
            if (winnerIndex < 0 || winnerIndex >= count || _Stores[winnerIndex] == null)
                return SaveDataResult<T>.Success(GetOrCreateTracked<T>(key), false);

            var winnerRead = fullReads[winnerIndex] ?? await _Stores[winnerIndex].GetAsync(key);
            if (!winnerRead.Found)
                return SaveDataResult<T>.Success(GetOrCreateTracked<T>(key), false);

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

        private async UniTask PersistAsync(ASaveDataObject saveDataObject)
        {
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
