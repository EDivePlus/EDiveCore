// Author: František Holubec
// Created: 09.02.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.UserCenter.SaveData;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager : ALoadableServiceBehaviour<UserCenterManager>
    {
        [SerializeField]
        private string _ServiceBaseUrl = "https://ediveplus.phil.muni.cz/api";
        
        [SerializeField]
        private string _AppSecret = "";
        
        private ISaveDataLocalStore _local;

        private string ServiceBaseUrl => (_ServiceBaseUrl ?? "").TrimEnd('/');

        private static int GetRequestTimeoutSeconds(int timeoutSeconds) => Mathf.Max(3, timeoutSeconds);

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _local ??= new PlayerPrefsSaveDataStore();
            return UniTask.CompletedTask;
        }
    }
}
