// Author: Michal Petr
// Created: 16.06.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public abstract class ASaveDataSyncHandler
    {
        protected abstract SaveDataDirtyFlag HandledFlags { get; }
        protected SaveDataService Service { get; private set; }
        protected ServiceHubSaveDataStore Store { get; private set; }
        protected SaveDataDomain Context { get; private set; }
        
        protected CancellationTokenSource _cts;

        public abstract event Action<(string Key, DateTime? UpdatedAt)> SyncSuccess;
        public abstract event Action<(string Key, string Error)> SyncFailure;

        private bool CanHandle(ASaveDataObject saveDataObject) => (saveDataObject.DirtyFlags & HandledFlags) != 0;

        protected static DateTime? Normalize(DateTime? value) =>
            value == null || value.Value == default ? null : value;

        public virtual void Initialize(SaveDataService service, ServiceHubSaveDataStore store, SaveDataDomain domain)
        {
            Terminate();
                
            Service = service;
            Store = store;
            Context = domain;
            
            _cts = new CancellationTokenSource();
        }
        
        public virtual void Terminate()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        
        public bool TryScheduleSync(ASaveDataObject saveDataObject, CancellationToken ct = default)
        {
            if (!CanHandle(saveDataObject))
                return false;
            
            saveDataObject.ClearDirty();
            
            try 
            {
                var json = JsonConvert.SerializeObject(saveDataObject);
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
        
        public virtual UniTask FlushAsync(CancellationToken ct = default) => UniTask.CompletedTask;
        
        public virtual void RemovePending(string key) { }
        
        protected UniTask<NetworkResponse<ApiResponse<SaveDataResponse>>> PutSaveDataAsync(
            SaveDataDomain context,
            string key,
            string json,
            CancellationToken ct)
        {
            var request = new SaveDataWriteRequest(json);
            return RestUtils.PutAsync<ApiResponse<SaveDataResponse>, SaveDataWriteRequest>(
                Store.KeyUrl(key),
                request,
                context.Auth.GetAccessToken(),
                null,
                Service.Settings.ApiTimeoutSeconds,
                ct
            );
        }

        protected UniTask<NetworkResponse<ApiResponse<SaveDataBatchResponse>>> PutSaveDataBatchAsync(
            SaveDataDomain context,
            IReadOnlyDictionary<string, string> entries,
            CancellationToken ct)
        {
            var request = new SaveDataBatchWriteRequest(entries);
            return RestUtils.PutAsync<ApiResponse<SaveDataBatchResponse>, SaveDataBatchWriteRequest>(
                Store.BatchUrl,
                request,
                context.Auth.GetAccessToken(),
                null,
                Service.Settings.ApiTimeoutSeconds,
                ct
            );
        }
    }
}
