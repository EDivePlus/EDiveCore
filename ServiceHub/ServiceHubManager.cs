// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Networking;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.SaveData;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
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
        
        private NetworkManager _networkManager;
        private MasterNetworkManager _masterNetworkManager;

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
            
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
                return;
            
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionStateChanged;
            _masterNetworkManager = await AppCore.Services.AwaitRegistered<MasterNetworkManager>();
            _masterNetworkManager.ServerPrepareHandlers += OnServerPrepareHandlers;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (_networkManager != null) 
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionStateChanged;
            if (_masterNetworkManager != null) 
                _masterNetworkManager.ServerPrepareHandlers -= OnServerPrepareHandlers;
        }

        private void OnServerConnectionStateChanged(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped && _networkManager.ServerManager.AreAllServersStopped())
            {
                // Flush save data to ensure all pending writes are completed
            }
        }
        
        private async UniTask OnServerPrepareHandlers()
        {
            // Authenticate server to backend
        }
    }
}
