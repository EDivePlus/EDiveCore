// Author: František Holubec
// Created: 08.04.2025

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Channel = FishNet.Transporting.Channel;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace EDIVE.Networking.Scenes
{
    public class NetworkSceneManager : AServiceBehaviour<NetworkSceneManager>
    {
        [SerializeField]
        [SceneReference]
        private List<string> _OnlineScenes;
        
        [SerializeField]
        [SceneReference]
        private string _ClientDefaultScene;
        
        [SerializeField]
        private bool _SetSceneActive = false;
        
        private NetworkManager _networkManager;
        private readonly List<Scene> _loadedScenes = new();
        
        public IReadOnlyList<Scene> LoadedScenes => _loadedScenes;

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

            _networkManager.ServerManager.RegisterBroadcast<ConnectionSceneRequest>(OnConnectionSceneRequest);

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

                _networkManager.ServerManager.UnregisterBroadcast<ConnectionSceneRequest>(OnConnectionSceneRequest);
            }
        }

        private void OnConnectionSceneRequest(NetworkConnection connection, ConnectionSceneRequest request, Channel channel)
        {
            if (request.Operation == ConnectionSceneRequestOperation.Load)
            {
                var loadData = new SceneLoadData(request.SceneName);
                _networkManager.SceneManager.LoadConnectionScenes(connection, loadData);
            }
            else
            {
                var unloadData = new SceneUnloadData(request.SceneName) {Options = new UnloadOptions {Mode = UnloadOptions.ServerUnloadMode.KeepUnused}};
                _networkManager.SceneManager.UnloadConnectionScenes(connection, unloadData);
            }
        }

        public async UniTask<Scene?> AwaitLoadConnectionScene(string sceneName)
        {
            if (_loadedScenes.TryGetFirst(s => s.name == sceneName, out var scene))
            {
                if (_networkManager.IsServerStarted) 
                    _networkManager.SceneManager.AddConnectionToScene(_networkManager.ClientManager.Connection, scene);
                
                return scene;
            }
            
            var completionSource = new UniTaskCompletionSource<Scene>();
            _networkManager.SceneManager.OnLoadEnd += OnConnectionSceneLoaded;
            _networkManager.ClientManager.Broadcast(new ConnectionSceneRequest(sceneName, ConnectionSceneRequestOperation.Load));
            
            var result = await completionSource.Task.TimeoutWithoutException(TimeSpan.FromSeconds(4));
            _networkManager.SceneManager.OnLoadEnd -= OnConnectionSceneLoaded;
            return result.IsTimeout ? null : result.Result;
            
            void OnConnectionSceneLoaded(SceneLoadEndEventArgs loadArgs)
            {
                if (loadArgs.LoadedScenes.TryGetFirst(s => s.name == sceneName, out var loadedScene))
                    completionSource.TrySetResult(loadedScene);
            }
        }
        
        public void UnloadConnectionScene(string sceneName)
        {
            if (_loadedScenes.All(s => s.name != sceneName))
                return;
            
            _networkManager.ClientManager.Broadcast(new ConnectionSceneRequest(sceneName, ConnectionSceneRequestOperation.Unload));
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

            if (_SetSceneActive)
            {
                var onlineScene = args.LoadedScenes.FirstOrDefault(loaded => _OnlineScenes.Any(online => loaded.name == GetSceneName(online)));
                if (onlineScene.isLoaded)
                {
                    UnitySceneManager.SetActiveScene(onlineScene);
                }
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
            if (string.IsNullOrEmpty(_ClientDefaultScene) || !authenticated)
                return;
            
            var onlineScene = GetSceneName(_ClientDefaultScene);
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
