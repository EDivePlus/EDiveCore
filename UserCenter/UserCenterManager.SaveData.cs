// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.UserCenter.Auth;
using EDIVE.UserCenter.SaveData;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using UnityEngine;
using ZLinq;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager
    {
        private readonly SemaphoreSlim _indexLock = new(1, 1);
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new(StringComparer.OrdinalIgnoreCase);
        
        private volatile bool _indexLoaded;
        private ConcurrentDictionary<string, SaveDataRecord> _index = new(StringComparer.OrdinalIgnoreCase);

        private void InvalidateIndex() => _indexLoaded = false;
        
        private string SaveDataBaseUrl()
        {
            var baseUrl = (_ServiceBaseUrl ?? "").TrimEnd('/');
            return $"{baseUrl}/savedata";
        }
        
        public async UniTask<SaveDataResult<T>> GetSaveData<T>(string key, CancellationToken ct = default, bool forceRefresh = false)
        {
            var effectiveToken = ct == CancellationToken.None ? this.GetCancellationTokenOnDestroy() : ct;
            
            if (IsLoggedIn)
            {
                var server = await GetJsonDataByKeyAsync(key, effectiveToken, forceRefresh);

                if (server.Success)
                {
                    if (JsonUtils.TryDeserializeObject<T>(server.Result, out var obj, out var derr))
                    {
                        _local.Set(key, server.Result);
                        return SaveDataResult<T>.Ok(obj, true);
                    }

                    return SaveDataResult<T>.Error($"Savedata JSON parse error: {derr}");
                }

                if (server.IsNotFound)
                {
                    if (_local.TryGet(key, out var lj) && JsonUtils.TryDeserializeObject<T>(lj, out var lo, out _))
                        return SaveDataResult<T>.Ok(lo, false);

                    return SaveDataResult<T>.NotFound();
                }
            }

            if (_local.TryGet(key, out var json))
            {
                if (JsonUtils.TryDeserializeObject<T>(json, out var localObj, out var lerr))
                    return SaveDataResult<T>.Ok(localObj, false);
                
                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(lerr))
                    return SaveDataResult<T>.Error($"Local JSON parse error: {lerr}");
            }

            return SaveDataResult<T>.NotFound();
        }

        public async UniTask<SaveDataResult<bool>> SetSaveData<T>(string key, T value, CancellationToken ct = default)
        {
            var effectiveToken = ct == CancellationToken.None ? this.GetCancellationTokenOnDestroy() : ct;
            var json = JsonConvert.SerializeObject(value);
            
            _local.Set(key, json);

            if (!IsLoggedIn) 
                return SaveDataResult<bool>.Ok(true, false);
            
            var up = await UpsertJsonDataByKeyAsync(key, json, effectiveToken);
            
            if (up.Success)
            {
                return SaveDataResult<bool>.Ok(true, true);
            }
            Debug.LogError($"Server save failed for key '{key}': {up.Error} (saved locally)");
            return SaveDataResult<bool>.Ok(true, false);
        }
        
        private static List<SaveDataRecord> TryParseList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            try
            {
                var span = raw.AsSpan().TrimStart();
                if (span.Length > 0 && span[0] == '{')
                {
                    var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SaveDataRecord>>(raw);
                    return wrapper?.Content;
                }

                return JsonConvert.DeserializeObject<List<SaveDataRecord>>(raw);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse save data list response: {e}");
                return null;
            }
        }

        private async UniTask<List<SaveDataRecord>> FetchAllRecordsAsync(CancellationToken ct)
        {
            return await FetchRecordsAsync(ct, page: null, size: 250);
        }
        
        private async UniTask<List<SaveDataRecord>> FetchRecordsAsync(CancellationToken ct, int? page = null, int size = 250)
        {
            var records = new List<SaveDataRecord>();
            var requestSinglePage = page.HasValue;
            var currentPage = page ?? 0;

            while (true)
            {
                var listUrl = $"{SaveDataBaseUrl()}?page={currentPage}&size={size}";
                var resp = await RestUtils.GetAsync<string>(
                    listUrl,
                    AuthStorage.GetAccessToken(),
                    GetBranchHeadersOrNull(),
                    GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                    ct
                );

                if (resp.IsNotFound) return records;
                if (!resp.Success) return null;

                var rows = TryParseList(resp.Raw);
                if (rows == null || rows.Count == 0) break;

                records.AddRange(rows);

                if (requestSinglePage || rows.Count < size) break;

                currentPage++;
            }

            return records;
        }

        private async UniTask EnsureIndexAsync(CancellationToken ct, bool forceRefresh)
        {
            if (!forceRefresh && _indexLoaded) return;

            await _indexLock.WaitAsync(ct);
            try
            {
                if (!forceRefresh && _indexLoaded) return;

                var rows = await FetchAllRecordsAsync(ct);
                if (rows == null) return; 

                var userUuid = AuthStorage.GetUserId();
        
                var newIndex = new ConcurrentDictionary<string, SaveDataRecord>(StringComparer.OrdinalIgnoreCase);

                foreach (var r in rows.AsValueEnumerable()
                             .Where(r => !string.IsNullOrWhiteSpace(r.Key))
                             .Where(r => string.IsNullOrEmpty(userUuid) || string.Equals(r.UserUuid, userUuid, StringComparison.OrdinalIgnoreCase)))
                {
                    newIndex[r.Key] = r;
                }

                _index = newIndex;
                _indexLoaded = true;
            }
            finally
            {
                _indexLock.Release();
            }
        }

        private async UniTask<NetworkResponse<string>> GetJsonDataByKeyAsync(string key, CancellationToken ct, bool forceRefresh)
        {
            await EnsureIndexAsync(ct, forceRefresh);

            if (!_indexLoaded)
                return NetworkResponse<string>.Fail(0, "index not loaded (network error)", raw: null);

            if (!_index.TryGetValue(key ?? "", out var rec) || rec == null || string.IsNullOrEmpty(rec.JsonData))
                return NetworkResponse<string>.Fail(404, "not found", raw: null);

            return NetworkResponse<string>.Ok(200, rec.JsonData, rec.JsonData);
        }
        
        private void UpsertLocalIndexCache(SaveDataRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.Key)) return;
            _index[record.Key] = record;
        }

        private async UniTask<NetworkResponse<SaveDataRecord>> UpsertJsonDataByKeyAsync(string key, string jsonData, CancellationToken ct)
        {
            await EnsureIndexAsync(ct, forceRefresh: false);

            var safeKey = key ?? string.Empty;
            var keyLock = _keyLocks.GetOrAdd(safeKey, _ => new SemaphoreSlim(1, 1));
    
            await keyLock.WaitAsync(ct);
            try
            {
                _index.TryGetValue(safeKey, out var existing);

                var userUuid = AuthStorage.GetUserId();

                if (existing != null && existing.ID > 0)
                {
                    var putUrl = SaveDataBaseUrl() + "/" + existing.ID;
                    var updatedRecord = await RestUtils.PutAsync<SaveDataRecord, object>(
                        putUrl,
                        new { key = safeKey, description = jsonData },
                        AuthStorage.GetAccessToken(),
                        GetBranchHeadersOrNull(),
                        GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                        ct
                    );
    
                    if (updatedRecord.Success && updatedRecord.Result != null) 
                    {
                        UpsertLocalIndexCache(updatedRecord.Result);
                    }
    
                    return updatedRecord;
                }

                object obj = string.IsNullOrEmpty(userUuid)
                    ? new { key = safeKey, description = jsonData }
                    : new { key = safeKey, description = jsonData, userUuid };

                var newRecord = await RestUtils.PostAsync<SaveDataRecord, object>(
                    SaveDataBaseUrl(),
                    obj,
                    AuthStorage.GetAccessToken(),
                    GetBranchHeadersOrNull(),
                    GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                    ct
                );

                if (newRecord.Success && newRecord.Result != null) 
                {
                    UpsertLocalIndexCache(newRecord.Result);
                }

                return newRecord;
            }
            finally
            {
                keyLock.Release();
            }
        }
    }
}
