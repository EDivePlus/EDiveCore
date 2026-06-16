// Author: Michal Petr
// Created: 16.06.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.ServiceHub.SaveData.SyncHandlers;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData
{
    [Serializable]
    public class ServiceHubSaveDataStore : ASaveDataStore
    {
        public override bool SupportsCheapTimestamp => true;

        public string BaseUrl => $"{SaveDataService.Settings.ServiceBaseUrl}/savedata/{Context.Key}";
        public string BatchUrl => $"{BaseUrl}/batch";
        public string KeyUrl(string key) => $"{BaseUrl}/{Uri.EscapeDataString(key)}";
        public string MetaUrl(string key) => $"{KeyUrl(key)}/meta";

        private List<ASaveDataSyncHandler> _handlers;
        private CancellationTokenSource _cts;

        public override void Initialize(SaveDataService saveDataService, SaveDataDomain domain)
        {
            base.Initialize(saveDataService, domain);

            _cts = new CancellationTokenSource();

            _handlers = new List<ASaveDataSyncHandler>
            {
                new ImmediateSaveDataSyncHandler(),
                new EndOfFrameSaveDataSyncHandler(),
                new BatchSaveDataSyncHandler()
            };

            _handlers.ForEach(handler =>
            {
                handler.Initialize(SaveDataService, this, domain);
                handler.SyncSuccess += OnSyncSuccess;
            });
        }

        private void OnSyncSuccess((string Key, DateTime? UpdatedAt) result)
        {
            Context.OnRemoteWriteConfirmed(result.Key, result.UpdatedAt);
        }

        public override async UniTask<StoreReadResult> GetAsync(string key)
        {
            if (!Context.Auth.IsValid())
                return StoreReadResult.NotFound;

            var response = await RestUtils.GetAsync<ApiResponse<SaveDataResponse>>(
                KeyUrl(key),
                Context.Auth.GetAccessToken(),
                null,
                SaveDataService.Settings.ApiTimeoutSeconds,
                _cts.Token
            );

            if (response.IsSuccess && response.Result is { Status: 0, Data: { Value: not null } data })
                return StoreReadResult.Hit(data.Value.ToString(Formatting.None), Normalize(data.UpdatedAt));

            if (!response.IsSuccess && !response.IsNotFound)
                Debug.LogWarning($"[ServiceHub] {Context.Key} get failed for '{key}': {response.ErrorMessage}");

            return StoreReadResult.NotFound;
        }

        public override async UniTask<TimestampPeek> GetTimestampAsync(string key)
        {
            if (!Context.Auth.IsValid())
                return TimestampPeek.Missing;

            var response = await RestUtils.GetAsync<ApiResponse<SaveDataResponse>>(
                MetaUrl(key),
                Context.Auth.GetAccessToken(),
                null,
                SaveDataService.Settings.ApiTimeoutSeconds,
                _cts.Token
            );

            if (response.IsSuccess && response.Result is { Status: 0, Data: { } data })
                return TimestampPeek.At(Normalize(data.UpdatedAt));

            if (!response.IsSuccess && !response.IsNotFound)
                Debug.LogWarning($"[ServiceHub] {Context.Key} meta get failed for '{key}': {response.ErrorMessage}");

            return TimestampPeek.Missing;
        }

        public override UniTask SetAsync(ASaveDataObject saveDataObject)
        {
            var handled = _handlers.Any(handler => handler.TryScheduleSync(saveDataObject));

            if (!handled)
                Debug.LogError($"No sync handler found for save data object with key {saveDataObject.Key} and dirty flags {saveDataObject.DirtyFlags}");

            return UniTask.CompletedTask;
        }

        public override async UniTask DeleteAsync(string key)
        {
            _handlers.ForEach(handler => handler.RemovePending(key));

            if (!Context.Auth.IsValid())
                return;

            var response = await RestUtils.DeleteAsync<string>(
                KeyUrl(key),
                Context.Auth.GetAccessToken(),
                null,
                SaveDataService.Settings.ApiTimeoutSeconds,
                _cts.Token
            );

            if (!response.IsSuccess && !response.IsNotFound)
                Debug.LogError($"[ServiceHub] {Context.Key} delete failed for '{key}': {response.ErrorMessage}");
        }

        public override async UniTask<bool> HasKeyAsync(string key)
        {
            var result = await GetAsync(key);
            return result.Found;
        }

        public override async UniTask FlushAsync()
        {
            foreach (var handler in _handlers)
                await handler.FlushAsync(_cts.Token);
        }

        private static DateTime? Normalize(DateTime value) => value == default ? null : value;
    }
}
