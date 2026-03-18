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
    }
}
