// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Networking;
using EDIVE.Networking.ServerManagement;
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
        
        [SerializeField]
        private ServerConfig _ServerConfig;
        
        private ISaveDataLocalStore _local;
        private ISaveDataLocalStore _serverLocal;

        private NetworkManager _networkManager;
        private MasterNetworkManager _masterNetworkManager;

        private string ServiceBaseUrl => (_ServiceBaseUrl ?? "").TrimEnd('/');

        private static int GetRequestTimeoutSeconds(int timeoutSeconds) => Mathf.Max(3, timeoutSeconds);
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            _local ??= new PlayerPrefsSaveDataStore();
            _serverLocal ??= CreateServerLocalStore(_ServerConfig != null ? _ServerConfig.ServerID : null);

            if (AuthStorage.Client.IsValid())
            {
                TryLoadStoredClientToken();
                await CheckClientAuthAsync(destroyCancellationToken);
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
                FlushAllServerDirtyEntries(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }
        
        private async UniTask OnServerPrepareHandlers()
        {
            // Try load server token from storage and check if valid
            if (AuthStorage.Server.IsValid())
            {
                TryLoadStoredServerToken();
                await CheckServerAuthAsync(destroyCancellationToken);
            }
            
            // If not available or invalid, try read from ServerConfig
            if (!IsServerLoggedIn && _ServerConfig && !string.IsNullOrEmpty(_ServerConfig.ServerID) && !string.IsNullOrEmpty(_ServerConfig.ServerSecret))
            {
                await LoginServerAsync(_ServerConfig.ServerID, _ServerConfig.ServerSecret, destroyCancellationToken);
                if (!IsServerLoggedIn)
                    Debug.LogError($"[ServiceHub] Server '{_ServerConfig.ServerID}' login from ServerConfig failed. Starting unauthenticated; server-scoped save data will not sync.");
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
