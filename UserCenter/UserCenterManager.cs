// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.UserCenter.SaveData;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager : ALoadableServiceBehaviour<UserCenterManager>
    {
        [SerializeField]
        private string _ServiceBaseUrl = "https://api.ediveplus.phil.muni.cz/service";
        
        [SerializeField]
        private string _BranchId = "2";
        
        [SerializeField]
        private string _ProfileKey = "player_profile_v1";
        
        private ISaveDataLocalStore _local;
        private Dictionary<string, string> _cachedBranchHeaders;

        private Dictionary<string, string> GetBranchHeadersOrNull()
        {
            if (string.IsNullOrWhiteSpace(_BranchId))
            {
                Debug.LogError("[UserCenterHttp] BranchId is NULL/EMPTY. Savedata endpoints will fail (branch is required).");
                return null;
            }

            _cachedBranchHeaders ??= new Dictionary<string, string>
            {
                {"branch-id", _BranchId}, {"branchId", _BranchId}, {"branch", _BranchId}, {"Branch-Id", _BranchId}, {"X-Branch-Id", _BranchId}
            };
            return _cachedBranchHeaders;
        }

        private static int GetRequestTimeoutSeconds(int timeoutSeconds) => Mathf.Max(3, timeoutSeconds);

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _local ??= new PlayerPrefsSaveDataStore();
            return UniTask.CompletedTask;
        }

        public async UniTask<DataResult<T>> GetData<T>(string key, CancellationToken ct = default, bool forceRefresh = false)
        {
            using var linkedCts = ct.CanBeCanceled 
                ? CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy()) 
                : null;
            var effectiveToken = linkedCts?.Token ?? this.GetCancellationTokenOnDestroy();
            
            if (IsLoggedIn)
            {
                var server = await GetDescriptionJsonByKeyAsync(key, effectiveToken, forceRefresh);

                if (server.Success)
                {
                    if (JsonUtils.TryDeserializeObject<T>(server.Result, out var obj, out var derr))
                    {
                        _local.Set(key, server.Result);
                        return DataResult<T>.Ok(obj, true);
                    }

                    return DataResult<T>.Error($"Savedata JSON parse error: {derr}");
                }

                if (server.IsNotFound)
                {
                    if (_local.TryGet(key, out var lj) && JsonUtils.TryDeserializeObject<T>(lj, out var lo, out _))
                        return DataResult<T>.Ok(lo, false);

                    return DataResult<T>.NotFound();
                }
            }

            if (_local.TryGet(key, out var json))
            {
                if (JsonUtils.TryDeserializeObject<T>(json, out var localObj, out var lerr))
                    return DataResult<T>.Ok(localObj, false);
                
                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(lerr))
                    return DataResult<T>.Error($"Local JSON parse error: {lerr}");
            }

            return DataResult<T>.NotFound();
        }

        public async UniTask<DataResult<bool>> SetData<T>(string key, T value, CancellationToken ct = default)
        {
            using var linkedCts = ct.CanBeCanceled 
                ? CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy()) 
                : null;
            var effectiveToken = linkedCts?.Token ?? this.GetCancellationTokenOnDestroy();

            var json = JsonConvert.SerializeObject(value);
            
            _local.Set(key, json);

            if (!IsLoggedIn) 
                return DataResult<bool>.Ok(true, false);
            
            var up = await UpsertDescriptionJsonByKeyAsync(key, json, effectiveToken);
           
            return up.Success 
                ? DataResult<bool>.Ok(true, true) 
                : DataResult<bool>.Error($"Server save failed: {up.Error} (saved locally)");
        }
    }
}
