// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.SaveData;
using EDIVE.Time.TimeSpanUtils;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    public partial class ServiceHubManager
    {
        private const int MAX_KEY_LENGTH = 256;
        private const int MAX_VALUE_BYTES = 256 * 1024;
        
        private const int BATCH_ENTRY_SIZE_THRESHOLD = 16 * 1024;
        private const int BATCH_TOTAL_SIZE_THRESHOLD = 128 * 1024;

        [SerializeField]
        [PropertyOrder(20)]
        [EnhancedBoxGroup("SaveData", Color = "@ColorTools.Orange", SpaceBefore = 8)]
        private UTimeSpan _DirtyDataSyncInterval = TimeSpan.FromSeconds(30);

        private readonly Dictionary<SaveDataDirtyFlag, Dictionary<string, string>> _dirtyEntries = new();
        private CancellationTokenSource _syncCts;

        private string SaveDataUserUrl => $"{ServiceBaseUrl}/savedata/user";
        private string SaveDataKeyUrl(string key) => $"{SaveDataUserUrl}/{Uri.EscapeDataString(key)}";
        private string SaveDataBatchUrl => $"{SaveDataUserUrl}/batch";

        private void OnEnable()
        {
            if (_syncCts != null)
                return;
            _syncCts = new CancellationTokenSource();

            StartSyncLoop(SaveDataDirtyFlag.OnBatch,
                ct => UniTask.Delay(_DirtyDataSyncInterval, cancellationToken: ct),
                _syncCts.Token).Forget();
            StartSyncLoop(SaveDataDirtyFlag.OnEndOfFrame,
                UniTask.WaitForEndOfFrame,
                _syncCts.Token).Forget();
        }

        private void OnDisable()
        {
            _syncCts?.Cancel();
            _syncCts?.Dispose();
            _syncCts = null;
        }

        private async UniTaskVoid StartSyncLoop(
            SaveDataDirtyFlag flag,
            Func<CancellationToken, UniTask> waitFactory,
            CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await waitFactory(ct);
                    await FlushDirtyEntries(flag, ct);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceHub] Sync loop error ({flag}): {e}");
                }
            }
        }

        public async UniTask<SaveDataResult<T>> GetSaveData<T>(
            string key,
            CancellationToken ct = default,
            bool forceRefresh = false)
        {
            var effectiveToken = ct == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : ct;

            // When not forcing a refresh, try local first
            if (!forceRefresh && _local.TryGet(key, out var cachedJson))
            {
                try
                {
                    var cachedObj = JsonConvert.DeserializeObject<T>(cachedJson);
                    return SaveDataResult<T>.Success(cachedObj, false);
                }
                catch (Exception ex)
                {
                    return SaveDataResult<T>.Error($"Local JSON parse error: {ex.Message}");
                }
            }

            if (IsLoggedIn)
            {
                var response = await RestUtils.GetAsync<ApiResponse<SaveDataResponse>>(
                    SaveDataKeyUrl(key),
                    AuthStorage.GetAccessToken(),
                    null,
                    GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                    effectiveToken
                );

                if (response.IsSuccess && response.Result is { Status: 0, Data: not null })
                {
                    try
                    {
                        var value = response.Result.Data.Value.ToObject<T>();
                        _local.Set(key, response.Result.Data.Value.ToString(Formatting.None));
                        _local.Save();
                        return SaveDataResult<T>.Success(value, true);
                    }
                    catch (Exception ex)
                    {
                        return SaveDataResult<T>.Error($"Deserialization error: {ex.Message}");
                    }
                }

                // Not found on server — fall through to local
                if (!response.IsNotFound && !response.IsSuccess)
                {
                    Debug.LogWarning($"[ServiceHub] Server get failed for '{key}': {response.ErrorMessage}");
                }
            }

            return SaveDataResult<T>.Error("Data not found");
        }

        public async UniTask<SaveDataStatus> SetSaveData<T>(
            string key,
            T value,
            SaveDataDirtyFlag flag = SaveDataDirtyFlag.Immediate,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > MAX_KEY_LENGTH)
            {
                Debug.LogWarning($"[ServiceHub] Invalid key: must be non-empty and at most {MAX_KEY_LENGTH} characters.");
                return SaveDataStatus.Error;
            }

            var effectiveToken = ct == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : ct;
            var json = JsonConvert.SerializeObject(value);

            if (Encoding.UTF8.GetByteCount(json) > MAX_VALUE_BYTES)
            {
                Debug.LogWarning($"[ServiceHub] Value for key '{key}' exceeds maximum size of {MAX_VALUE_BYTES / 1024} KB.");
                return SaveDataStatus.Error;
            }

            _local.Set(key, json);
            _local.Save();

            if (flag == SaveDataDirtyFlag.Immediate)
            {
                if (!IsLoggedIn)
                    return SaveDataStatus.SavedLocal;

                var response = await PutSaveDataAsync(key, json, effectiveToken);

                if (response.IsSuccess)
                    return SaveDataStatus.Saved;

                Debug.LogError($"[ServiceHub] Server save failed for '{key}': {response.ErrorMessage} (saved locally)");
                return SaveDataStatus.SavedLocal;
            }

            if (flag != SaveDataDirtyFlag.NoChange)
            {
                if (!_dirtyEntries.TryGetValue(flag, out var bucket))
                {
                    bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _dirtyEntries[flag] = bucket;
                }

                bucket[key] = json;
            }
            
            return SaveDataStatus.SavedLocal;
        }

        /// <summary>
        /// Flushes all entries marked with <see cref="SaveDataDirtyFlag.Manual"/>.
        /// </summary>
        public UniTask FlushManualEntries(CancellationToken ct = default)
        {
            var effectiveToken = ct == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : ct;
            return FlushDirtyEntries(SaveDataDirtyFlag.Manual, effectiveToken);
        }

        public async UniTask<SaveDataResult<bool>> DeleteSaveData(
            string key,
            CancellationToken ct = default)
        {
            var effectiveToken = ct == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : ct;

            _local.Delete(key);
            _local.Save();

            // Remove from all dirty buckets so a pending PUT doesn't resurrect the key
            foreach (var bucket in _dirtyEntries.Values)
                bucket.Remove(key);

            if (!IsLoggedIn)
                return SaveDataResult<bool>.Success(true, false);

            var response = await RestUtils.DeleteAsync<string>(
                SaveDataKeyUrl(key),
                AuthStorage.GetAccessToken(),
                null,
                GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                effectiveToken
            );

            if (response.IsSuccess || response.IsNotFound)
                return SaveDataResult<bool>.Success(true, true);

            Debug.LogError($"[ServiceHub] Server delete failed for '{key}': {response.ErrorMessage}");
            return SaveDataResult<bool>.Error(response.ErrorMessage);
        }

        public async UniTask FlushAllDirtyEntries(CancellationToken ct = default)
        {
            if (!IsLoggedIn)
                return;

            var effectiveToken = ct == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : ct;

            var flags = new List<SaveDataDirtyFlag>(_dirtyEntries.Keys);
            foreach (var flag in flags)
                await FlushDirtyEntries(flag, effectiveToken);
        }

        private async UniTask FlushDirtyEntries(SaveDataDirtyFlag flag, CancellationToken ct)
        {
            if (!_dirtyEntries.TryGetValue(flag, out var bucket) || bucket.Count == 0)
                return;

            if (!IsLoggedIn)
                return;

            // Snapshot and clear so new writes during flush go into the next cycle
            var snapshot = new Dictionary<string, string>(bucket, StringComparer.OrdinalIgnoreCase);
            bucket.Clear();

            // Partition entries: large ones go as single PUTs, small ones are batched.
            var batch = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var batchByteSize = 0;

            foreach (var (key, json) in snapshot)
            {
                if (ct.IsCancellationRequested)
                {
                    RequeueRemaining(bucket, snapshot, alreadyHandled: null, skipKey: key);
                    bucket.TryAdd(key, json);
                    return;
                }

                var byteSize = Encoding.UTF8.GetByteCount(json);
                if (byteSize > BATCH_ENTRY_SIZE_THRESHOLD)
                {
                    var response = await PutSaveDataAsync(key, json, ct);
                    if (!response.IsSuccess)
                    {
                        Debug.LogError($"[ServiceHub] Sync failed for '{key}': {response.ErrorMessage}");
                        bucket.TryAdd(key, json);
                    }
                    continue;
                }

                // Flush the accumulated batch before it grows too large.
                if (batch.Count > 0 && batchByteSize + byteSize > BATCH_TOTAL_SIZE_THRESHOLD)
                {
                    await FlushBatchAsync(batch, bucket, ct);
                    batch.Clear();
                    batchByteSize = 0;
                }

                batch[key] = json;
                batchByteSize += byteSize;
            }

            if (batch.Count > 0)
                await FlushBatchAsync(batch, bucket, ct);
        }

        private static void RequeueRemaining(
            Dictionary<string, string> bucket,
            Dictionary<string, string> snapshot,
            HashSet<string> alreadyHandled,
            string skipKey)
        {
            foreach (var (k, v) in snapshot)
            {
                if (k == skipKey) continue;
                if (alreadyHandled != null && alreadyHandled.Contains(k)) continue;
                bucket.TryAdd(k, v);
            }
        }

        private async UniTask FlushBatchAsync(
            Dictionary<string, string> batch,
            Dictionary<string, string> bucket,
            CancellationToken ct)
        {
            if (batch.Count == 1)
            {
                // No point batching a single entry — issue it directly.
                using var e = batch.GetEnumerator();
                e.MoveNext();
                var (key, json) = (e.Current.Key, e.Current.Value);
                var single = await PutSaveDataAsync(key, json, ct);
                if (!single.IsSuccess)
                {
                    Debug.LogError($"[ServiceHub] Sync failed for '{key}': {single.ErrorMessage}");
                    bucket.TryAdd(key, json);
                }
                return;
            }

            var response = await PutSaveDataBatchAsync(batch, ct);

            if (!response.IsSuccess || response.Result is not { Status: 0, Data: not null })
            {
                var err = response.IsSuccess
                    ? response.Result?.Message ?? "Unknown error"
                    : response.ErrorMessage;
                Debug.LogError($"[ServiceHub] Batch sync failed ({batch.Count} entries): {err}");
                foreach (var (k, v) in batch)
                    bucket.TryAdd(k, v);
                return;
            }

            // Per-key errors come back in `errors`; re-queue only those so
            // successfully saved keys aren't resent on the next cycle.
            var errors = response.Result.Data.Errors;
            if (errors is { Count: > 0 })
            {
                foreach (var (k, msg) in errors)
                {
                    Debug.LogError($"[ServiceHub] Batch sync rejected '{k}': {msg}");
                    if (batch.TryGetValue(k, out var v))
                        bucket.TryAdd(k, v);
                }
            }
        }

        private UniTask<NetworkResponse<ApiResponse<SaveDataResponse>>> PutSaveDataAsync(
            string key,
            string json,
            CancellationToken ct)
        {
            var request = new SaveDataWriteRequest(json);
            return RestUtils.PutAsync<ApiResponse<SaveDataResponse>, SaveDataWriteRequest>(
                SaveDataKeyUrl(key),
                request,
                AuthStorage.GetAccessToken(),
                null,
                GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                ct
            );
        }

        private UniTask<NetworkResponse<ApiResponse<SaveDataBatchResponse>>> PutSaveDataBatchAsync(
            IReadOnlyDictionary<string, string> entries,
            CancellationToken ct)
        {
            var request = new SaveDataBatchWriteRequest(entries);
            return RestUtils.PutAsync<ApiResponse<SaveDataBatchResponse>, SaveDataBatchWriteRequest>(
                SaveDataBatchUrl,
                request,
                AuthStorage.GetAccessToken(),
                null,
                GetRequestTimeoutSeconds(_ApiTimeoutSeconds),
                ct
            );
        }
    }
}
