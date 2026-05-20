// Author: František Holubec
// Created: 08.04.2025

using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.OdinExtensions.Attributes;
using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.Networking.Scenes
{
    public class NetworkSceneManager : ALoadableServiceBehaviour<NetworkSceneManager>
    {
        [SerializeField]
        [SceneReference]
        private List<string> _GlobalScenes = new();

        [SerializeField]
        private float _JoinTimeout = 5f;

        private NetworkManager _networkManager;
        
        private readonly Dictionary<string, SceneID> _serverLoadedScenes = new();
        private readonly Dictionary<string, UniTaskCompletionSource<SceneID>> _serverPendingLoads = new();

        public IEnumerable<Scene> LoadedScenes => EnumerateLoadedScenes(includeGlobals: true);
        public IEnumerable<Scene> LoadedLocalScenes => EnumerateLoadedScenes(includeGlobals: false);

        
        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = NetworkManager.main;
            _networkManager.onServerConnectionState += OnServerConnectionState;
            _networkManager.Subscribe<ConnectionSceneRequest>(OnConnectionSceneRequest, asServer: true);
            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            dependencies.Add(typeof(MasterNetworkManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_networkManager == null) return;
            
            _networkManager.onServerConnectionState -= OnServerConnectionState;
            _networkManager.Unsubscribe<ConnectionSceneRequest>(OnConnectionSceneRequest, asServer: true);
            _networkManager.sceneModule.onSceneLoaded -= OnServerSceneLoaded;
        }
        
        public async UniTask<Scene?> AwaitJoinScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) 
                return null;
            
            var tcs = new UniTaskCompletionSource<Scene>();

            void OnLoaded(SceneID id, bool asServer)
            {
                if (_networkManager.sceneModule.TryGetSceneState(id, out var state) &&
                    state.scene.IsValid() &&
                    state.scene.isLoaded &&
                    state.scene.name == sceneName)
                {
                    tcs.TrySetResult(state.scene);
                }
            }

            _networkManager.sceneModule.onSceneLoaded += OnLoaded;
            try
            {
                if (TryFindLoadedScene(sceneName, out var existing))
                    return existing;
                
                _networkManager.SendToServer(new ConnectionSceneRequest(sceneName, ConnectionSceneRequestOperation.Join));

                var result = await tcs.Task.TimeoutWithoutException(TimeSpan.FromSeconds(_JoinTimeout));
                if (result.IsTimeout)
                {
                    Debug.LogWarning($"[NetworkSceneManager] Timed out waiting for scene '{sceneName}' to load.", this);
                    return null;
                }
                return result.Result;
            }
            finally
            {
                _networkManager.sceneModule.onSceneLoaded -= OnLoaded;
            }
        }
        
        public void LeaveScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (_networkManager == null) return;

            _networkManager.SendToServer(new ConnectionSceneRequest(sceneName, ConnectionSceneRequestOperation.Leave));
        }

        private void OnConnectionSceneRequest(PlayerID sender, ConnectionSceneRequest request, bool asServer)
        {
            if (!asServer) return;

            switch (request.Operation)
            {
                case ConnectionSceneRequestOperation.Join:
                    ServerJoinScene(sender, request.SceneName).Forget();
                    break;
                case ConnectionSceneRequestOperation.Leave:
                    ServerLeaveScene(sender, request.SceneName);
                    break;
            }
        }

        private async UniTask ServerJoinScene(PlayerID player, string sceneName)
        {
            try
            {
                var sceneId = await ServerEnsureSceneLoaded(sceneName, isPublic: false);
                if (sceneId == null) return;

                if (_networkManager.TryGetModule<ScenePlayersModule>(true, out var scenePlayers))
                    scenePlayers.AddPlayerToScene(player, sceneId.Value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ServerLeaveScene(PlayerID player, string sceneName)
        {
            if (!_serverLoadedScenes.TryGetValue(sceneName, out var sceneId))
                return;

            if (!_networkManager.TryGetModule<ScenePlayersModule>(true, out var scenePlayers))
                return;

            scenePlayers.RemovePlayerFromScene(player, sceneId);

            if (IsGlobalScene(sceneName))
                return;
            
            if (scenePlayers.TryGetPlayersAttachedToScene(sceneId, out var remaining) && remaining.Count == 0)
            {
                _networkManager.sceneModule.UnloadSceneAsync(sceneId);
                _serverLoadedScenes.Remove(sceneName);
            }
        }

        private async UniTask<SceneID?> ServerEnsureSceneLoaded(string sceneName, bool isPublic)
        {
            if (_serverLoadedScenes.TryGetValue(sceneName, out var existing))
                return existing;

            if (_serverPendingLoads.TryGetValue(sceneName, out var pending))
            {
                try { return await pending.Task; }
                catch { return null; }
            }

            var tcs = new UniTaskCompletionSource<SceneID>();
            _serverPendingLoads[sceneName] = tcs;

            var op = _networkManager.sceneModule.LoadSceneAsync(sceneName, new PurrSceneSettings
            {
                mode = LoadSceneMode.Additive,
                isPublic = isPublic
            });

            if (op == null)
            {
                _serverPendingLoads.Remove(sceneName);
                tcs.TrySetCanceled();
                return null;
            }

            try { return await tcs.Task; }
            catch { return null; }
        }

        private void OnServerConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                HookServerSceneEvents();
                LoadGlobalScenes();
            }
            else if (state == ConnectionState.Disconnected)
            {
                foreach (var pending in _serverPendingLoads.Values)
                    pending.TrySetCanceled();
                _serverPendingLoads.Clear();
                _serverLoadedScenes.Clear();
            }
        }
        
        private void HookServerSceneEvents()
        {
            var module = _networkManager.sceneModule;
            if (module == null) return;
            module.onSceneLoaded -= OnServerSceneLoaded;
            module.onSceneLoaded += OnServerSceneLoaded;
        }

        private void LoadGlobalScenes()
        {
            var module = _networkManager.sceneModule;
            if (module == null) return;

            foreach (var sceneRef in _GlobalScenes)
            {
                var sceneName = GetSceneName(sceneRef);
                if (string.IsNullOrEmpty(sceneName)) continue;
                ServerEnsureSceneLoaded(sceneName, isPublic: true).Forget();
            }
        }

        private void OnServerSceneLoaded(SceneID id, bool asServer)
        {
            if (!asServer) return;

            var module = _networkManager.sceneModule;
            if (module == null || !module.TryGetSceneState(id, out var state))
                return;

            if (IsBootstrapScene(state.scene))
                return;

            _serverLoadedScenes[state.scene.name] = id;

            if (_serverPendingLoads.TryGetValue(state.scene.name, out var tcs))
            {
                tcs.TrySetResult(id);
                _serverPendingLoads.Remove(state.scene.name);
            }
        }
        
        private IEnumerable<Scene> EnumerateLoadedScenes(bool includeGlobals)
        {
            foreach (var state in _networkManager.sceneModule.sceneStates.Values)
            {
                var scene = state.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (IsBootstrapScene(scene)) continue;
                if (!includeGlobals && IsGlobalScene(scene.name)) continue;
                yield return scene;
            }
        }

        private bool TryFindLoadedScene(string sceneName, out Scene scene)
        {
            var module = _networkManager?.sceneModule;
            if (module != null)
            {
                foreach (var state in module.sceneStates.Values)
                {
                    var stateScene = state.scene;
                    if (stateScene.name != sceneName || !stateScene.IsValid() || !stateScene.isLoaded)
                        continue;
                    
                    scene = stateScene;
                    return true;
                }
            }
            scene = default;
            return false;
        }

        private bool IsBootstrapScene(Scene scene)
        {
            if (scene.name == "DontDestroyOnLoad") return true;
            if (_networkManager != null && _networkManager.gameObject.scene == scene) return true;
            return false;
        }

        private bool IsGlobalScene(string sceneName)
        {
            foreach (var g in _GlobalScenes)
            {
                if (!string.IsNullOrEmpty(g) && GetSceneName(g) == sceneName)
                    return true;
            }
            return false;
        }

        private static string GetSceneName(string fullPath)
        {
            return string.IsNullOrEmpty(fullPath) ? fullPath : Path.GetFileNameWithoutExtension(fullPath);
        }

    }
}
