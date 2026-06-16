// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.ServiceHub.Auth;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData
{
    public class SaveDataService : MonoBehaviour, IServiceHubModule
    {
        private const int MAX_KEY_LENGTH = 256;
        private const int MAX_VALUE_BYTES = 256 * 1024;

        private const int BATCH_ENTRY_SIZE_THRESHOLD = 16 * 1024;
        private const int BATCH_TOTAL_SIZE_THRESHOLD = 128 * 1024;

        [SerializeField]
        [Required]
        private ClientAuthService _ClientAuth;

        [SerializeField]
        [Required]
        private ServerAuthService _ServerAuth;

        private ServiceHubSettings _settings;

        private ISaveDataLocalStore _local;
        private ISaveDataLocalStore _serverLocal;

        private readonly Dictionary<SaveDataDirtyFlag, Dictionary<string, string>> _dirtyEntries = new();
        private readonly Dictionary<SaveDataDirtyFlag, Dictionary<string, string>> _serverDirtyEntries = new();
        private readonly Dictionary<string, object> _objectCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> _serverObjectCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _dirtyLock = new();
        private readonly object _serverDirtyLock = new();
        private CancellationTokenSource _syncCts;

        private SaveDataContext _userCtx;
        private SaveDataContext _serverCtx;
        private bool _contextsReady;
        private bool _initialized;
        private bool _authSubscribed;

        public void Initialize(ServiceHubSettings settings)
        {
            _settings = settings;

            _local = AuthStorage.Client.IsValid()
                ? new PlayerPrefsSaveDataStore($"uc.savedata.{ClientAuthService.GetUserId()}.")
                : new PlayerPrefsSaveDataStore();

            if (AuthStorage.Server.IsValid())
            {
                var serverId = ServerAuthService.GetServerId();
                if (!string.IsNullOrEmpty(serverId))
                    _serverLocal = new PlayerPrefsSaveDataStore($"uc.savedata.server.{serverId}.");
            }

            SubscribeAuth();
            _initialized = true;
            StartSyncLoops();
        }

        private void SubscribeAuth()
        {
            if (_authSubscribed) return;
            _ClientAuth.OnLoginSucceeded += OnClientLoginSucceeded;
            _ClientAuth.OnLoggedOut += OnClientLoggedOut;
            _ServerAuth.OnLoginSucceeded += OnServerLoginSucceeded;
            _authSubscribed = true;
        }

        private void UnsubscribeAuth()
        {
            if (!_authSubscribed) return;
            if (_ClientAuth != null)
            {
                _ClientAuth.OnLoginSucceeded -= OnClientLoginSucceeded;
                _ClientAuth.OnLoggedOut -= OnClientLoggedOut;
            }
            if (_ServerAuth != null)
                _ServerAuth.OnLoginSucceeded -= OnServerLoginSucceeded;
            _authSubscribed = false;
        }

        private void OnClientLoginSucceeded(LoginResponse response)
        {
            var userId = response?.AuthUser?.Id ?? ClientAuthService.GetUserId();
            _local = new PlayerPrefsSaveDataStore(string.IsNullOrEmpty(userId)
                ? "uc.savedata."
                : $"uc.savedata.{userId}.");
            _objectCache.Clear();
            _contextsReady = false;
        }

        private void OnClientLoggedOut()
        {
            _local = new PlayerPrefsSaveDataStore();
            _objectCache.Clear();
            _contextsReady = false;
        }

        private void OnServerLoginSucceeded(ServerLoginResponse response)
        {
            var serverId = response?.ServerId ?? ServerAuthService.GetServerId();
            if (string.IsNullOrEmpty(serverId))
            {
                Debug.LogError("[ServiceHub] Server login response missing ServerId; skipping server local store init.");
                return;
            }
            _serverLocal = new PlayerPrefsSaveDataStore($"uc.savedata.server.{serverId}.");
            _serverObjectCache.Clear();
            _contextsReady = false;
        }

        private void EnsureContexts()
        {
            if (_contextsReady) return;
            _userCtx = new SaveDataContext(this, isServer: false, $"{_settings.ServiceBaseUrl}/savedata/user", _dirtyEntries, _dirtyLock, "user");
            _serverCtx = new SaveDataContext(this, isServer: true, $"{_settings.ServiceBaseUrl}/savedata/server", _serverDirtyEntries, _serverDirtyLock, "server");
            _contextsReady = true;
        }

        private SaveDataContext UserCtx() { EnsureContexts(); return _userCtx; }
        private SaveDataContext ServerCtx() { EnsureContexts(); return _serverCtx; }

        private int RequestTimeoutSeconds => _settings.ApiTimeoutSeconds;

        private void StartSyncLoops()
        {
            if (_syncCts != null) return;
            _syncCts = new CancellationTokenSource();

            StartSyncLoop(SaveDataDirtyFlag.OnBatch,
                ct => UniTask.Delay(_settings.DirtyDataSyncInterval, cancellationToken: ct),
                _syncCts.Token).Forget();
            StartSyncLoop(SaveDataDirtyFlag.OnEndOfFrame,
                UniTask.WaitForEndOfFrame,
                _syncCts.Token).Forget();
        }

        private void OnDestroy()
        {
            _syncCts?.Cancel();
            _syncCts?.Dispose();
            _syncCts = null;
            UnsubscribeAuth();
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
                    if (!_initialized) continue;
                    await FlushDirtyEntries(UserCtx(), flag, ct);
                    await FlushDirtyEntries(ServerCtx(), flag, ct);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.LogError($"[ServiceHub] Sync loop error ({flag}): {e}");
                }
            }
        }

        // ---- User-scoped public API ----

        public UniTask<SaveDataResult<T>> GetSaveData<T>(string key, CancellationToken ct = default, bool forceRefresh = false)
            => GetSaveDataInternal<T>(UserCtx(), key, ct, forceRefresh);

        public UniTask<SaveDataStatus> SetSaveData<T>(string key, T value, SaveDataDirtyFlag flag = SaveDataDirtyFlag.Immediate, CancellationToken ct = default)
            => SetSaveDataInternal(UserCtx(), key, value, flag, ct);

        public UniTask<SaveDataResult<bool>> DeleteSaveData(string key, CancellationToken ct = default)
            => DeleteSaveDataInternal(UserCtx(), key, ct);

        public UniTask FlushManualEntries(CancellationToken ct = default)
            => FlushDirtyEntries(UserCtx(), SaveDataDirtyFlag.Manual, ct);

        public UniTask FlushAllDirtyEntries(CancellationToken ct = default)
            => FlushAllDirtyEntriesInternal(UserCtx(), ct);

        // ---- Server-scoped public API ----

        public UniTask<SaveDataResult<T>> GetServerSaveData<T>(string key, CancellationToken ct = default, bool forceRefresh = false)
            => GetSaveDataInternal<T>(ServerCtx(), key, ct, forceRefresh);

        public UniTask<SaveDataStatus> SetServerSaveData<T>(string key, T value, SaveDataDirtyFlag flag = SaveDataDirtyFlag.Immediate, CancellationToken ct = default)
            => SetSaveDataInternal(ServerCtx(), key, value, flag, ct);

        public UniTask<SaveDataResult<bool>> DeleteServerSaveData(string key, CancellationToken ct = default)
            => DeleteSaveDataInternal(ServerCtx(), key, ct);

        public UniTask FlushServerManualEntries(CancellationToken ct = default)
            => FlushDirtyEntries(ServerCtx(), SaveDataDirtyFlag.Manual, ct);

        public UniTask FlushAllServerDirtyEntries(CancellationToken ct = default)
            => FlushAllDirtyEntriesInternal(ServerCtx(), ct);

        // ---- Internals (context-driven) ----

        private async UniTask<SaveDataResult<T>> GetSaveDataInternal<T>(
            SaveDataContext ctx,
            string key,
            CancellationToken ct,
            bool forceRefresh)
        {
            var local = ctx.Local;
            var cache = ctx.ObjectCache;

            if (!forceRefresh && cache.TryGetValue(key, out var cachedInstance) && cachedInstance is T typedInstance)
                return SaveDataResult<T>.Success(typedInstance, false);

            if (!forceRefresh && local.TryGet(key, out var cachedJson))
            {
                try
                {
                    var cachedObj = JsonConvert.DeserializeObject<T>(cachedJson);
                    cache[key] = cachedObj;
                    return SaveDataResult<T>.Success(cachedObj, false);
                }
                catch (Exception ex)
                {
                    return SaveDataResult<T>.Error($"Local JSON parse error: {ex.Message}");
                }
            }

            if (ctx.IsAuthValid)
            {
                var response = await RestUtils.GetAsync<ApiResponse<SaveDataResponse>>(
                    ctx.KeyUrl(key),
                    ctx.AccessToken,
                    null,
                    RequestTimeoutSeconds,
                    ct
                );

                if (response.IsSuccess && response.Result is { Status: 0, Data: not null })
                {
                    try
                    {
                        var value = response.Result.Data.Value.ToObject<T>();
                        local.Set(key, response.Result.Data.Value.ToString(Formatting.None));
                        local.Save();
                        cache[key] = value;
                        return SaveDataResult<T>.Success(value, true);
                    }
                    catch (Exception ex)
                    {
                        return SaveDataResult<T>.Error($"Deserialization error: {ex.Message}");
                    }
                }

                if (!response.IsNotFound && !response.IsSuccess)
                {
                    Debug.LogWarning($"[ServiceHub] {ctx.LogScope} get failed for '{key}': {response.ErrorMessage}");
                }
            }

            if (forceRefresh && local.TryGet(key, out var fallbackJson))
            {
                try
                {
                    var fallbackObj = JsonConvert.DeserializeObject<T>(fallbackJson);
                    cache[key] = fallbackObj;
                    return SaveDataResult<T>.Success(fallbackObj, false);
                }
                catch
                {
                    // fall through to "Data not found"
                }
            }

            return SaveDataResult<T>.Error("Data not found");
        }

        private async UniTask<SaveDataStatus> SetSaveDataInternal<T>(
            SaveDataContext ctx,
            string key,
            T value,
            SaveDataDirtyFlag flag,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > MAX_KEY_LENGTH)
            {
                Debug.LogWarning($"[ServiceHub] Invalid key: must be non-empty and at most {MAX_KEY_LENGTH} characters.");
                return SaveDataStatus.Error;
            }

            var json = JsonConvert.SerializeObject(value);

            if (Encoding.UTF8.GetByteCount(json) > MAX_VALUE_BYTES)
            {
                Debug.LogWarning($"[ServiceHub] Value for key '{key}' exceeds maximum size of {MAX_VALUE_BYTES / 1024} KB.");
                return SaveDataStatus.Error;
            }

            var local = ctx.Local;
            local.Set(key, json);
            local.Save();
            ctx.ObjectCache[key] = value;
            
            if (value is ASaveDataObject saveDataObject)
                saveDataObject.ClearDirty();

            if (flag == SaveDataDirtyFlag.Immediate)
            {
                if (!ctx.IsAuthValid)
                    return SaveDataStatus.SavedLocal;

                try
                {
                    var response = await PutSaveDataAsync(ctx, key, json, ct);
                    if (response.IsSuccess)
                        return SaveDataStatus.Saved;

                    Debug.LogError($"[ServiceHub] {ctx.LogScope} save failed for '{key}': {response.ErrorMessage} (saved locally)");
                    return SaveDataStatus.SavedLocal;
                }
                catch (OperationCanceledException)
                {
                    return SaveDataStatus.SavedLocal;
                }
            }

            if (flag != SaveDataDirtyFlag.NoChange)
            {
                lock (ctx.DirtyLock)
                {
                    if (!ctx.DirtyEntries.TryGetValue(flag, out var bucket))
                    {
                        bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        ctx.DirtyEntries[flag] = bucket;
                    }
                    bucket[key] = json;
                }
            }

            return SaveDataStatus.SavedLocal;
        }

        private async UniTask<SaveDataResult<bool>> DeleteSaveDataInternal(
            SaveDataContext ctx,
            string key,
            CancellationToken ct)
        {
            var local = ctx.Local;
            local.Delete(key);
            local.Save();
            ctx.ObjectCache.Remove(key);

            lock (ctx.DirtyLock)
            {
                foreach (var bucket in ctx.DirtyEntries.Values)
                    bucket.Remove(key);
            }

            if (!ctx.IsAuthValid)
                return SaveDataResult<bool>.Success(true, false);

            var response = await RestUtils.DeleteAsync<string>(
                ctx.KeyUrl(key),
                ctx.AccessToken,
                null,
                RequestTimeoutSeconds,
                ct
            );

            if (response.IsSuccess || response.IsNotFound)
                return SaveDataResult<bool>.Success(true, true);

            Debug.LogError($"[ServiceHub] {ctx.LogScope} delete failed for '{key}': {response.ErrorMessage}");
            return SaveDataResult<bool>.Error(response.ErrorMessage);
        }

        private async UniTask FlushAllDirtyEntriesInternal(SaveDataContext ctx, CancellationToken ct)
        {
            if (!ctx.IsAuthValid)
                return;

            List<SaveDataDirtyFlag> flags;
            lock (ctx.DirtyLock)
            {
                flags = new List<SaveDataDirtyFlag>(ctx.DirtyEntries.Keys);
            }
            foreach (var flag in flags)
                await FlushDirtyEntries(ctx, flag, ct);
        }

        private async UniTask FlushDirtyEntries(SaveDataContext ctx, SaveDataDirtyFlag flag, CancellationToken ct)
        {
            if (!ctx.IsAuthValid)
                return;

            Dictionary<string, string> bucket;
            Dictionary<string, string> snapshot;
            lock (ctx.DirtyLock)
            {
                if (!ctx.DirtyEntries.TryGetValue(flag, out bucket) || bucket.Count == 0)
                    return;

                snapshot = new Dictionary<string, string>(bucket, StringComparer.OrdinalIgnoreCase);
                bucket.Clear();
            }

            var batch = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var batchByteSize = 0;

            foreach (var (key, json) in snapshot)
            {
                if (ct.IsCancellationRequested)
                {
                    lock (ctx.DirtyLock)
                    {
                        RequeueRemaining(bucket, snapshot, alreadyHandled: null, skipKey: key);
                        bucket.TryAdd(key, json);
                    }
                    return;
                }

                var byteSize = Encoding.UTF8.GetByteCount(json);
                if (byteSize > BATCH_ENTRY_SIZE_THRESHOLD)
                {
                    var response = await PutSaveDataAsync(ctx, key, json, ct);
                    if (!response.IsSuccess)
                    {
                        Debug.LogError($"[ServiceHub] {ctx.LogScope} sync failed for '{key}': {response.ErrorMessage}");
                        lock (ctx.DirtyLock) bucket.TryAdd(key, json);
                    }
                    continue;
                }

                if (batch.Count > 0 && batchByteSize + byteSize > BATCH_TOTAL_SIZE_THRESHOLD)
                {
                    await FlushBatchAsync(ctx, batch, bucket, ct);
                    batch.Clear();
                    batchByteSize = 0;
                }

                batch[key] = json;
                batchByteSize += byteSize;
            }

            if (batch.Count > 0)
                await FlushBatchAsync(ctx, batch, bucket, ct);
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
            SaveDataContext ctx,
            Dictionary<string, string> batch,
            Dictionary<string, string> bucket,
            CancellationToken ct)
        {
            if (batch.Count == 1)
            {
                using var e = batch.GetEnumerator();
                e.MoveNext();
                var (key, json) = (e.Current.Key, e.Current.Value);
                var single = await PutSaveDataAsync(ctx, key, json, ct);
                if (!single.IsSuccess)
                {
                    Debug.LogError($"[ServiceHub] {ctx.LogScope} sync failed for '{key}': {single.ErrorMessage}");
                    lock (ctx.DirtyLock) bucket.TryAdd(key, json);
                }
                return;
            }

            var response = await PutSaveDataBatchAsync(ctx, batch, ct);

            if (!response.IsSuccess || response.Result is not { Status: 0, Data: not null })
            {
                var err = response.IsSuccess
                    ? response.Result?.Message ?? "Unknown error"
                    : response.ErrorMessage;
                Debug.LogError($"[ServiceHub] {ctx.LogScope} batch sync failed ({batch.Count} entries): {err}");
                lock (ctx.DirtyLock)
                {
                    foreach (var (k, v) in batch)
                        bucket.TryAdd(k, v);
                }
                return;
            }

            var errors = response.Result.Data.Errors;
            if (errors is { Count: > 0 })
            {
                foreach (var (k, msg) in errors)
                    Debug.LogError($"[ServiceHub] {ctx.LogScope} batch sync rejected '{k}': {msg}");

                lock (ctx.DirtyLock)
                {
                    foreach (var (k, _) in errors)
                    {
                        if (batch.TryGetValue(k, out var v))
                            bucket.TryAdd(k, v);
                    }
                }
            }
        }

        private UniTask<NetworkResponse<ApiResponse<SaveDataResponse>>> PutSaveDataAsync(
            SaveDataContext ctx,
            string key,
            string json,
            CancellationToken ct)
        {
            var request = new SaveDataWriteRequest(json);
            return RestUtils.PutAsync<ApiResponse<SaveDataResponse>, SaveDataWriteRequest>(
                ctx.KeyUrl(key),
                request,
                ctx.AccessToken,
                null,
                RequestTimeoutSeconds,
                ct
            );
        }

        private UniTask<NetworkResponse<ApiResponse<SaveDataBatchResponse>>> PutSaveDataBatchAsync(
            SaveDataContext ctx,
            IReadOnlyDictionary<string, string> entries,
            CancellationToken ct)
        {
            var request = new SaveDataBatchWriteRequest(entries);
            return RestUtils.PutAsync<ApiResponse<SaveDataBatchResponse>, SaveDataBatchWriteRequest>(
                ctx.BatchUrl,
                request,
                ctx.AccessToken,
                null,
                RequestTimeoutSeconds,
                ct
            );
        }

        private readonly struct SaveDataContext
        {
            private readonly SaveDataService _service;
            private readonly bool _isServer;

            public readonly string RemoteBaseUrl;
            public readonly Dictionary<SaveDataDirtyFlag, Dictionary<string, string>> DirtyEntries;
            public readonly object DirtyLock;
            public readonly string LogScope;

            public SaveDataContext(
                SaveDataService service,
                bool isServer,
                string remoteBaseUrl,
                Dictionary<SaveDataDirtyFlag, Dictionary<string, string>> dirtyEntries,
                object dirtyLock,
                string logScope)
            {
                _service = service;
                _isServer = isServer;
                RemoteBaseUrl = remoteBaseUrl;
                DirtyEntries = dirtyEntries;
                DirtyLock = dirtyLock;
                LogScope = logScope;
            }

            public ISaveDataLocalStore Local => _isServer ? _service._serverLocal : _service._local;
            public Dictionary<string, object> ObjectCache => _isServer ? _service._serverObjectCache : _service._objectCache;
            public bool IsAuthValid => _isServer ? AuthStorage.Server.IsValid() : AuthStorage.Client.IsValid();
            public string AccessToken => _isServer ? AuthStorage.Server.GetAccessToken() : AuthStorage.Client.GetAccessToken();
            public string KeyUrl(string key) => $"{RemoteBaseUrl}/{Uri.EscapeDataString(key)}";
            public string BatchUrl => $"{RemoteBaseUrl}/batch";
        }
    }
}
