// Author: František Holubec
// Created: 09.02.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.UserCenter.Auth;
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

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _local ??= new PlayerPrefsSaveDataStore();

            if (AuthStorage.IsValid())
            {
                TryLoadStoredToken();
                await CheckAuthAsync(destroyCancellationToken);
            }
        }
    }
}
