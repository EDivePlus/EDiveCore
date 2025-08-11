// Author: František Holubec
// Created: 08.04.2025

using System.IO;
using EDIVE.Core.Services;
using EDIVE.OdinExtensions.Attributes;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace EDIVE.Networking.Scenes
{
    // Simple single scene net manager
    // Todo: Allow multiple scenes, load/unload content based on player state, use some interest management from Mirror 
    public class NetworkSceneManager : AServiceBehaviour<NetworkSceneManager>
    {
        [SerializeField]
        [SceneReference]
        private string _OfflineScene;
        
        [SerializeField]
        [SceneReference]
        private string _OnlineScene;
        
        private NetworkManager _networkManager;

        private void OnEnable()
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null || !_networkManager.Initialized)
                return;
            
            if (_OnlineScene == string.Empty || _OfflineScene == string.Empty)
            {
                NetworkManagerExtensions.LogWarning("Online or Offline scene is not specified. Default scenes will not load.");
                return;
            }

            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;
            LoadOfflineScene();
        }

        private void OnDisable()
        {
            if (!ApplicationState.IsQuitting() && _networkManager != null && _networkManager.Initialized)
            {
                _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
                _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
                _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
            }
        }
        
        private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs obj)
        {
            var onlineLoaded = false;
            foreach (var s in obj.LoadedScenes)
            {
                if (s.name == GetSceneName(_OnlineScene))
                {
                    onlineLoaded = true;
                    break;
                }
            }
            
            if (onlineLoaded)
                UnloadOfflineScene();
        }
        
        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            if (obj.ConnectionState == LocalConnectionState.Started)
            {
                if (!_networkManager.ServerManager.IsOnlyOneServerStarted())
                    return;
                
                SceneLoadData sld = new(GetSceneName(_OnlineScene)) { ReplaceScenes = ReplaceOption.None };
                _networkManager.SceneManager.LoadGlobalScenes(sld);
            }
            else if (obj.ConnectionState == LocalConnectionState.Stopped && !_networkManager.ServerManager.IsAnyServerStarted())
            {
                LoadOfflineScene();
            }
        }
        
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            if (obj.ConnectionState == LocalConnectionState.Stopped)
            {
                LoadOfflineScene();
            }
        }
        
        private void LoadOfflineScene()
        {
            if (UnitySceneManager.GetActiveScene().name == GetSceneName(_OfflineScene))
                return;
            
            UnitySceneManager.LoadScene(_OfflineScene, LoadSceneMode.Additive);
        }
        
        private void UnloadOfflineScene()
        {
            var s = UnitySceneManager.GetSceneByName(GetSceneName(_OfflineScene));
            if (string.IsNullOrEmpty(s.name))
                return;

            UnitySceneManager.UnloadSceneAsync(s);
        }
        
        private string GetSceneName(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
