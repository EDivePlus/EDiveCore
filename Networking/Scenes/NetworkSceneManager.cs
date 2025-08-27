// Author: František Holubec
// Created: 08.04.2025

using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.Core.Services;
using EDIVE.OdinExtensions.Attributes;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace EDIVE.Networking.Scenes
{
    public class NetworkSceneManager : AServiceBehaviour<NetworkSceneManager>
    {
        [SerializeField]
        [SceneReference]
        private List<string> _OnlineScenes;
        
        private NetworkManager _networkManager;
        private readonly List<Scene> _loadedScenes = new();

        private void OnEnable()
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null || !_networkManager.Initialized)
                return;

            if (_OnlineScenes.All(string.IsNullOrEmpty))
            {
                NetworkManagerExtensions.LogWarning("No online scene is not specified. Default scenes will not load.");
                return;
            }

            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _networkManager.ServerManager.OnAuthenticationResult += OnAuthenticationResult;
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
            UnloadOnlineScenes();
        }
        
        private void OnDisable()
        {
            if (!ApplicationState.IsQuitting() && _networkManager != null && _networkManager.Initialized)
            {
                _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
                _networkManager.ServerManager.OnAuthenticationResult -= OnAuthenticationResult;
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
            }
        }
        
        private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
        {
            // Cache newly loaded scenes, clear invalid ones
            _loadedScenes.AddRange(args.LoadedScenes);
            for (var i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                var scene = _loadedScenes[i];
                if (!scene.IsValid() || !scene.isLoaded) 
                    _loadedScenes.RemoveAt(i);
            }
        }
        
        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started && _networkManager.ServerManager.IsOnlyOneServerStarted())
            {
                var allOnlineScenes = _OnlineScenes.Select(GetSceneName).ToList();
                var loadData = new SceneLoadData(allOnlineScenes) {Options = new LoadOptions {AutomaticallyUnload = false}};
                _networkManager.SceneManager.LoadConnectionScenes(loadData);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped && _networkManager.ServerManager.AreAllServersStopped())
            {
                UnloadOnlineScenes();
            }
        }
        
        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                UnloadOnlineScenes();
            }
        }
        
        private void OnAuthenticationResult(NetworkConnection connection, bool authenticated)
        {
            if (!authenticated)
                return;

            var onlineScene = GetSceneName(_OnlineScenes.FirstOrDefault());
            var loadData = new SceneLoadData(onlineScene);
            _networkManager.SceneManager.LoadConnectionScenes(connection, loadData);
        }
        
        private void UnloadOnlineScenes()
        {
            foreach (var scene in _loadedScenes)
            {
                if (scene.IsValid() && scene.isLoaded) 
                    UnitySceneManager.UnloadSceneAsync(scene);
            }
            _loadedScenes.Clear();
        }
        
        private string GetSceneName(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
