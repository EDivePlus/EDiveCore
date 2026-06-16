// Author: Michal Petr
// Created: 16.06.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public abstract class APeriodicSaveDataSyncHandler : ASaveDataSyncHandler
    {
        private readonly Dictionary<string, string> _syncBucket = new(StringComparer.Ordinal);
        private readonly object _syncBucketLock = new();

        public override event Action<(string Key, DateTime? UpdatedAt)> SyncSuccess;
        public override event Action<(string Key, string Error)> SyncFailure;

        protected abstract UniTask WaitForSync(CancellationToken ct);

        public override void Initialize(SaveDataService service, ServiceHubSaveDataStore store, SaveDataDomain domain)
        {
            base.Initialize(service, store, domain);
            UniTask.Void(() => SyncLoop(_cts.Token));
        }

        protected override void ScheduleSync(string key, string json)
        {
            lock (_syncBucketLock)
            {
                _syncBucket[key] = json;
            }
        }

        public override UniTask FlushAsync(CancellationToken ct = default) => FlushSyncBucket(ct);

        public override void RemovePending(string key)
        {
            lock (_syncBucketLock)
            {
                _syncBucket.Remove(key);
            }
        }

        private async UniTaskVoid SyncLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await WaitForSync(ct);
                    await FlushSyncBucket(ct);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private async UniTask FlushSyncBucket(CancellationToken ct)
        {
            if (!Context.Auth.IsValid())
                return;

            Dictionary<string, string> snapshot;

            lock (_syncBucketLock)
            {
                if (_syncBucket.Count == 0)
                    return;

                snapshot = new Dictionary<string, string>(_syncBucket, StringComparer.Ordinal);
                _syncBucket.Clear();
            }

            var batch = new Dictionary<string, string>(StringComparer.Ordinal);
            var batchByteSize = 0;

            foreach (var (key, json) in snapshot)
            {
                if (ct.IsCancellationRequested)
                    RequeueUnflushed(snapshot);
                
                var jsonByteSize = Encoding.UTF8.GetByteCount(json);
                
                if (batch.Count > 0 && batchByteSize + jsonByteSize > SaveDataService.BATCH_MAX_SIZE)
                {
                    await FlushBatchAsync(batch, ct);
                    batch.Clear();
                    batchByteSize = 0;
                }
                
                batch[key] = json;
                batchByteSize += jsonByteSize;
            }
            
            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch, ct);
            }
        }

        private void RequeueUnflushed(Dictionary<string, string> unflushed)
        {
            lock (_syncBucketLock)
            {
                unflushed.ForEach(kvp => _syncBucket.TryAdd(kvp.Key, kvp.Value));
            }
        }

        private async UniTask FlushBatchAsync(Dictionary<string, string> batch, CancellationToken ct)
        {
            if (batch.Count == 1)
            {
                var kvp = batch.First();
                var result = await PutSaveDataAsync(Context, kvp.Key, kvp.Value, ct);
                if (result.IsSuccess)
                    SyncSuccess?.Invoke((kvp.Key, Normalize(result.Result?.Data?.UpdatedAt)));
                else
                    SyncFailure?.Invoke((kvp.Key, result.ErrorMessage));
                return;
            }
            
            var response = await PutSaveDataBatchAsync(Context, batch, ct);

            if (!response.IsSuccess || response.Result?.Data == null || response.Result.Status != 0)
            {
                var err = response.IsSuccess
                    ? response.Result?.Message ?? "Unknown error"
                    : response.ErrorMessage;
                
                batch.Keys.ForEach(k => SyncFailure?.Invoke((k, err)));
                
                lock (_syncBucketLock)
                {
                    batch.ForEach(kvp => _syncBucket.TryAdd(kvp.Key, kvp.Value));
                }
                return;
            }

            var data = response.Result.Data;

            if (data.Saved != null)
                data.Saved.ForEach(s => SyncSuccess?.Invoke((s.Key, Normalize(s.UpdatedAt))));

            var errors = data.Errors;
            if (errors != null && errors.Count > 0)
            {
                var failedKeys = errors.Keys.ToArray();
                failedKeys.ForEach(k => SyncFailure?.Invoke((k, errors[k])));

                lock (_syncBucketLock)
                {
                    failedKeys.ForEach(k => _syncBucket.TryAdd(k, batch[k]));
                }
            }
        }
    }
}
