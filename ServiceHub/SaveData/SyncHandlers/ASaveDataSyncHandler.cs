// Author: Michal Petr
// Created: 16.06.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public abstract class ASaveDataSyncHandler
    {
        private static readonly TimeSpan MIN_RETRY_DELAY = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan MAX_RETRY_DELAY = TimeSpan.FromSeconds(60);

        protected abstract SaveDataDirtyFlag HandledFlags { get; }
        protected SaveDataService Service { get; private set; }
        protected ServiceHubSaveDataStore Store { get; private set; }
        protected SaveDataDomain Context { get; private set; }

        protected CancellationTokenSource _cts;

        // Failed writes, retried with backoff
        private readonly Dictionary<string, string> _retryBucket = new(StringComparer.Ordinal);
        private readonly object _retryLock = new();
        private TimeSpan _retryDelay = MIN_RETRY_DELAY;

        public event Action<(string Key, DateTime? UpdatedAt)> SyncSuccess;
        public event Action<(string Key, string Error)> SyncFailure;

        private bool CanHandle(ASaveDataObject saveDataObject) => (saveDataObject.DirtyFlags & HandledFlags) != 0;

        protected static DateTime? Normalize(DateTime? value) =>
            value == null || value.Value == default ? null : value;

        protected void RaiseSyncSuccess(string key, DateTime? updatedAt)
        {
            SaveDataRejections.Remove(Context.Key, key);
            SyncSuccess?.Invoke((key, Normalize(updatedAt)));
        }
        protected void RaiseSyncFailure(string key, string error) => SyncFailure?.Invoke((key, error));

        public virtual void Initialize(SaveDataService service, ServiceHubSaveDataStore store, SaveDataDomain domain)
        {
            Terminate();

            Service = service;
            Store = store;
            Context = domain;

            _cts = new CancellationTokenSource();
            UniTask.Void(() => RetryLoop(_cts.Token));
        }

        public virtual void Terminate()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public bool TryScheduleSync(ASaveDataObject saveDataObject, CancellationToken ct = default)
        {
            if (!CanHandle(saveDataObject))
                return false;

            saveDataObject.ClearDirty();

            try
            {
                var json = JsonConvert.SerializeObject(saveDataObject);
                // Newer value replaces pending retry
                RemoveRetry(saveDataObject.Key);
                ScheduleSync(saveDataObject.Key, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize save data object with key {saveDataObject.Key}: {e}");
                return false;
            }
        }

        protected abstract void ScheduleSync(string key, string json);

        public virtual UniTask FlushAsync(CancellationToken ct = default) => FlushRetriesAsync(ct);

        public virtual void RemovePending(string key) => RemoveRetry(key);

        // Drop queued writes, on logout so they don't go out under next token
        public virtual void ClearPending()
        {
            lock (_retryLock)
            {
                _retryBucket.Clear();
            }
        }

        // Bad request 4xx won't fix itself, auth/timeout/rate limit can
        protected static bool IsRetryable(long statusCode) =>
            statusCode < 400 || statusCode >= 500 || statusCode == 401 || statusCode == 403 || statusCode == 408 || statusCode == 429;

        // Queues a retry when it can help, otherwise the key stays unsaved until its value changes
        protected bool HandleWriteFailure(string key, string json, long statusCode, int apiStatus, string message)
        {
            RaiseSyncFailure(key, message);
            // Service off keeps it queued, the flush waits until it is back
            if (apiStatus == (int) ServiceHubStatus.ServiceDisabled || !SaveDataRejections.IsPermanent(apiStatus) && IsRetryable(statusCode))
            {
                QueueRetry(key, json);
                return true;
            }

            var reason = SaveDataRejections.Describe(apiStatus, message);
            Debug.LogError($"[ServiceHub] {Context.Key} save '{key}' not saved to the server ({statusCode}, {apiStatus}): {reason}. Kept locally, sent again once it changes.");
            SaveDataRejections.Add(new SaveDataRejection(Context.Key, key, reason));
            return false;
        }

        protected void QueueRetry(string key, string json)
        {
            lock (_retryLock)
            {
                _retryBucket.TryAdd(key, json);
            }
        }

        private void RemoveRetry(string key)
        {
            lock (_retryLock)
            {
                _retryBucket.Remove(key);
            }
        }

        private async UniTaskVoid RetryLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(_retryDelay, cancellationToken: ct);
                    var failedAgain = await FlushRetriesAsync(ct);
                    _retryDelay = failedAgain
                        ? TimeSpan.FromTicks(Math.Min(_retryDelay.Ticks * 2, MAX_RETRY_DELAY.Ticks))
                        : MIN_RETRY_DELAY;
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // True when some write failed again
        private async UniTask<bool> FlushRetriesAsync(CancellationToken ct)
        {
            if (Context == null || !Context.CanSync)
                return false;
            KeyValuePair<string, string>[] snapshot;
            lock (_retryLock)
            {
                if (_retryBucket.Count == 0)
                    return false;
                snapshot = _retryBucket.ToArray();
                _retryBucket.Clear();
            }
            var failed = false;
            foreach (var (key, json) in snapshot)
            {
                if (ct.IsCancellationRequested)
                {
                    QueueRetry(key, json);
                    continue;
                }
                var result = await PutSaveDataAsync(Context, key, json, ct);
                if (result.IsSuccess && result.Result is { Status: 0 })
                {
                    RaiseSyncSuccess(key, result.Result.Data?.UpdatedAt);
                    continue;
                }
                if (HandleWriteFailure(key, json, result.StatusCode, result.Result?.Status ?? 0, result.ErrorMessage ?? result.Result?.Message))
                    failed = true;
            }
            return failed;
        }

        protected async UniTask<NetworkResponse<ApiResponse<SaveDataResponse>>> PutSaveDataAsync(
            SaveDataDomain context,
            string key,
            string json,
            CancellationToken ct)
        {
            var request = new SaveDataWriteRequest(json);
            var response = await context.Auth.SendAsync(token => RestUtils.PutAsync<ApiResponse<SaveDataResponse>, SaveDataWriteRequest>(
                Store.KeyUrl(key),
                request,
                token,
                null,
                Service.Settings.ApiTimeoutSeconds,
                ct
            ));
            AppServices.HandleStatus(response.Result?.Status ?? 0, AppService.SaveData);
            return response;
        }

        protected async UniTask<NetworkResponse<ApiResponse<SaveDataBatchResponse>>> PutSaveDataBatchAsync(
            SaveDataDomain context,
            IReadOnlyDictionary<string, string> entries,
            CancellationToken ct)
        {
            var request = new SaveDataBatchWriteRequest(entries);
            var response = await context.Auth.SendAsync(token => RestUtils.PutAsync<ApiResponse<SaveDataBatchResponse>, SaveDataBatchWriteRequest>(
                Store.BatchUrl,
                request,
                token,
                null,
                Service.Settings.ApiTimeoutSeconds,
                ct
            ));
            AppServices.HandleStatus(response.Result?.Status ?? 0, AppService.SaveData);
            return response;
        }
    }
}
