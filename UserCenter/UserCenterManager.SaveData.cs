// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Time.TimeSpanUtils;
using EDIVE.UserCenter.Auth;
using EDIVE.UserCenter.SaveData;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager
    {
        private const int MAX_KEY_LENGTH = 256;
        private const int MAX_VALUE_BYTES = 256 * 1024;

        [SerializeField]
        [PropertyOrder(20)]
        [EnhancedBoxGroup("SaveData", Color = "@ColorTools.Orange", SpaceBefore = 8)]
        private UTimeSpan _DirtyDataSyncInterval = TimeSpan.FromSeconds(30);

        private readonly Dictionary<SaveDataDirtyFlag, Dictionary<string, string>> _dirtyEntries = new();
        private CancellationTokenSource _syncCts;

        private string SaveDataUserUrl => $"{ServiceBaseUrl}/savedata/user";
        private string SaveDataKeyUrl(string key) => $"{SaveDataUserUrl}/{Uri.EscapeDataString(key)}";

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
                    Debug.LogError($"[UserCenter] Sync loop error ({flag}): {e}");
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
                    Debug.LogWarning($"[UserCenter] Server get failed for '{key}': {response.ErrorMessage}");
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
                Debug.LogWarning($"[UserCenter] Invalid key: must be non-empty and at most {MAX_KEY_LENGTH} characters.");
                return SaveDataStatus.Error;
            }

            var effectiveToken = ct == CancellationToken.None
                ? this.GetCancellationTokenOnDestroy()
                : ct;
            var json = JsonConvert.SerializeObject(value);

            if (Encoding.UTF8.GetByteCount(json) > MAX_VALUE_BYTES)
            {
                Debug.LogWarning($"[UserCenter] Value for key '{key}' exceeds maximum size of {MAX_VALUE_BYTES / 1024} KB.");
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

                Debug.LogError($"[UserCenter] Server save failed for '{key}': {response.ErrorMessage} (saved locally)");
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

            Debug.LogError($"[UserCenter] Server delete failed for '{key}': {response.ErrorMessage}");
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
            var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bucket.Clear();

            foreach (var (key, json) in snapshot)
            {
                if (ct.IsCancellationRequested)
                {
                    // Re-queue remaining entries so they aren't lost
                    foreach (var remaining in snapshot)
                    {
                        if (!bucket.ContainsKey(remaining.Key) && !processedKeys.Contains(remaining.Key))
                            bucket[remaining.Key] = remaining.Value;
                    }
                    break;
                }

                var response = await PutSaveDataAsync(key, json, ct);
                if (!response.IsSuccess)
                {
                    Debug.LogError($"[UserCenter] Sync failed for '{key}': {response.ErrorMessage}");
                    bucket.TryAdd(key, json);
                }
                else
                {
                    processedKeys.Add(key);
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
    }
}
