// Author: František Holubec
// Created: 08.04.2025

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using Sirenix.OdinInspector;
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
        private HashSet<Scene> _loadedScenes = new();

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

            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
            LoadOfflineScene();
        }
        
        private void OnDisable()
        {
            if (!ApplicationState.IsQuitting() && _networkManager != null && _networkManager.Initialized)
            {
                _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
            }
        }
        
        private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
        {
            _loadedScenes.AddRange(args.LoadedScenes);
            if (args.LoadedScenes.TryGetFirst(s => s.name == GetSceneName(_OnlineScene), out var onlineScene))
            {
                UnloadOfflineScene();
                UnitySceneManager.SetActiveScene(onlineScene);
            }
            if (args.LoadedScenes.Any(s => s.name == GetSceneName(_OnlineScene)))
            {
                UnloadOfflineScene();
            }
        }

        
        private void OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            if (obj.ConnectionState == LocalConnectionState.Started)
            {
                if (!_networkManager.ServerManager.IsOnlyOneServerStarted())
                    return;

                var lookup = new SceneLookupData(GetSceneName(_OnlineScene));
                SceneLoadData data = new(lookup);
                _networkManager.SceneManager.LoadGlobalScenes(data);
            }
            else if (obj.ConnectionState == LocalConnectionState.Stopping)
            {
                SceneUnloadData data = new(GetSceneName(_OnlineScene));
                _networkManager.SceneManager.UnloadGlobalScenes(data);
            }
        }
        
        private void OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            if (obj.ConnectionState == LocalConnectionState.Stopped)
            {
                foreach (var scene in _loadedScenes)
                {
                    if (scene.IsValid() && scene.isLoaded) 
                        UnitySceneManager.UnloadSceneAsync(scene);
                }
                _loadedScenes.Clear();
                LoadOfflineScene();
            }
        }
        
        private void LoadOfflineScene()
        {
            var offlineSceneName = GetSceneName(_OfflineScene);
            var offlineScene = UnitySceneManager.GetSceneByName(offlineSceneName);
            if (offlineScene.isLoaded)
                return;
            
            UniTask.Void(async () =>
            {
                await UnitySceneManager.LoadSceneAsync(offlineSceneName, LoadSceneMode.Additive);
                UnitySceneManager.SetActiveScene(UnitySceneManager.GetSceneByName(offlineSceneName));
            });
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
