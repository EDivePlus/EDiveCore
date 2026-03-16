// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.UserCenter.SaveData;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
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

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _local ??= new PlayerPrefsSaveDataStore(prefix: "uc.savedata.");
            return UniTask.CompletedTask;
        }

        private CancellationToken GetEffectiveCancellationToken(CancellationToken ct)
        {
            return ct.CanBeCanceled ? ct : this.GetCancellationTokenOnDestroy();
        }

        private async UniTask<DataResult<T>> GetData<T>(string key, CancellationToken ct = default, bool forceRefresh = false)
        {
            ct = GetEffectiveCancellationToken(ct);
            
            if (IsLoggedIn)
            {
                var server = await GetDescriptionJsonByKeyAsync(key, ct, forceRefresh);

                if (server.Success)
                {
                    if (JsonUtils.TryDeserializeObject<T>(server.Result, out var obj, out var derr))
                    {
                        _local.Set(key, server.Result);
                        return DataResult<T>.Ok(obj, fromServer: true, fromLocal: false, fromMemory: false);
                    }

                    return DataResult<T>.Error($"Savedata JSON parse error: {derr}");
                }

                if (server.IsNotFound)
                {
                    if (_local.TryGet(key, out var lj) && JsonUtils.TryDeserializeObject<T>(lj, out var lo, out _))
                        return DataResult<T>.Ok(lo, fromServer: false, fromLocal: true, fromMemory: false);

                    return DataResult<T>.NotFound();
                }

                // network/server fail → fallback local
            }

            // Local fallback
            if (_local.TryGet(key, out var json))
            {
                if (JsonUtils.TryDeserializeObject<T>(json, out var localObj, out var lerr))
                    return DataResult<T>.Ok(localObj, fromServer: false, fromLocal: true, fromMemory: false);
                
                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(lerr))
                    return DataResult<T>.Error($"Local JSON parse error: {lerr}");
            }

            return DataResult<T>.NotFound();
        }

        private async UniTask<DataResult<bool>> SetData<T>(string key, T value, CancellationToken ct = default)
        {
            ct = GetEffectiveCancellationToken(ct);

            var json = JsonConvert.SerializeObject(value);
            
            _local.Set(key, json);

            if (!IsLoggedIn) 
                return DataResult<bool>.Ok(true, fromServer: false, fromLocal: true, fromMemory: false);
            
            var up = await UpsertDescriptionJsonByKeyAsync(key, json, ct);
           
            return up.Success 
                ? DataResult<bool>.Ok(true, fromServer: true, fromLocal: true, fromMemory: false) 
                : DataResult<bool>.Error($"Server save failed: {up.Error} (saved locally)"); // soft-fail
        }

        public UniTask<DataResult<PlayerProfileJson>> GetPlayerProfileJson(CancellationToken ct = default, bool forceRefresh = false)
            => GetData<PlayerProfileJson>(_ProfileKey, ct, forceRefresh);

        public UniTask<DataResult<bool>> SetPlayerProfileJson(PlayerProfileJson pj, CancellationToken ct = default)
            => SetData(_ProfileKey, pj, ct);


        [EnhancedBoxGroup("Debug", Color = "@ColorTools.Orange", SpaceBefore = 8)]
        [PropertyOrder(999)]
        [Button("Get Profile Json")]
        private async void DebugGetProfile()
        {
            try
            {
                if (!Application.isPlaying) return;
                var r = await GetPlayerProfileJson();
                Debug.Log($"[UserCenter][Profile][GET] status={r.Status} fromServer={r.FromServer} fromLocal={r.FromLocal} err={r.ErrorMessage} val={JsonUtility.ToJson(r.Value)}");
            }
            catch (Exception e) 
            {
                Debug.LogError($"[UserCenter][Profile][GET] exception: {e}");
            }
        }

        [EnhancedBoxGroup("Debug")]
        [PropertyOrder(999)]
        [Button("Set Profile Json (random)")]
        private async void DebugSetProfile()
        {
            try
            {
                if (!Application.isPlaying) return;
                var pj = new PlayerProfileJson ($"User_{UnityEngine.Random.Range(1000, 9999)}", "default");
                var r = await SetPlayerProfileJson(pj);
                Debug.Log($"[UserCenter][Profile][SET] status={r.Status} err={r.ErrorMessage}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserCenter][Profile][SET] exception: {e}");
            }
        }
    }
}
