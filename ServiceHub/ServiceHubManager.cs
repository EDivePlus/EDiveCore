// Author: František Holubec
// Created: 09.02.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.SaveData;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    public partial class ServiceHubManager : ALoadableServiceBehaviour<ServiceHubManager>
    {
        [SerializeField]
        private string _ServiceBaseUrl = "https://ediveplus.phil.muni.cz/api";
        
        [SerializeField]
        private string _AppSecret = "";
        
        private ISaveDataLocalStore _local;
        private ISaveDataLocalStore _serverLocal;

        private string ServiceBaseUrl => (_ServiceBaseUrl ?? "").TrimEnd('/');

        private static int GetRequestTimeoutSeconds(int timeoutSeconds) => Mathf.Max(3, timeoutSeconds);
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _local ??= new PlayerPrefsSaveDataStore();

            if (AuthStorage.Client.IsValid())
            {
                TryLoadStoredClientToken();
                await CheckClientAuthAsync(destroyCancellationToken);
            }
        }

        private static PlayerPrefsSaveDataStore CreateServerLocalStore(string serverId)
        {
            if (!string.IsNullOrEmpty(serverId)) 
                return new PlayerPrefsSaveDataStore($"uc.savedata.server.{serverId}.");
            Debug.LogError("[ServiceHub] ServerConfig does not have a valid ServerID");
            return null;
        }
    }
}
