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
using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace EDIVE.Networking.Scenes
{
    /// <summary>
    /// PurrNet-flavoured scene manager. All server-loaded scenes are public by default and
    /// replicate to every connected player (including history replay for late joiners), so
    /// FishNet's "global vs connection" split doesn't survive the migration:
    /// _GlobalScene/_OnlineScenes/_ClientDefaultScene all just get loaded on the server when
    /// it comes online, and clients receive them automatically.
    /// </summary>
    public class NetworkSceneManager : AServiceBehaviour<NetworkSceneManager>
    {
        [SerializeField]
        [SceneReference]
        private string _GlobalScene;

        [SerializeField]
        [SceneReference]
        private List<string> _OnlineScenes;

        [SerializeField]
        [SceneReference]
        [Tooltip("PurrNet replays scene history to new joiners, so this is now equivalent to adding the scene to _OnlineScenes. Kept for backwards compatibility.")]
        private string _ClientDefaultScene;

        [SerializeField]
        private bool _SetSceneActive = false;

        private NetworkManager _networkManager;
        private readonly List<Scene> _loadedScenes = new();
        // Scene names we've already passed to LoadSceneAsync. Tracks in-flight loads so a
        // second request during the gap before onSceneLoaded fires doesn't trigger a duplicate
        // load — that's how PurrNet's _idToScene ends up with two SceneIDs for one Unity scene.
        private readonly HashSet<string> _requestedSceneNames = new();
        public Scene GlobalScene { get; private set; }

        public IEnumerable<Scene> LoadedScenes => _loadedScenes;
        public IEnumerable<Scene> LoadedConnectionScenes => _loadedScenes.Where(s => s != GlobalScene);

        protected override void OnEnable()
        {
            base.OnEnable();
            _networkManager = NetworkManager.main;
            if (_networkManager == null)
                return;

            if (_OnlineScenes.All(string.IsNullOrEmpty) && string.IsNullOrEmpty(_GlobalScene) && string.IsNullOrEmpty(_ClientDefaultScene))
            {
                Debug.LogWarning("[NetworkSceneManager] No online scenes specified — default scenes will not load.");
                return;
            }

            _networkManager.onClientConnectionState += OnClientConnectionState;
            _networkManager.onServerConnectionState += OnServerConnectionState;
            _networkManager.Subscribe<ConnectionSceneRequest>(OnServerConnectionSceneRequest, asServer: true);

            TrySubscribeSceneEvents();
            UnloadOnlineScenes();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_networkManager == null)
                return;

            _networkManager.onClientConnectionState -= OnClientConnectionState;
            _networkManager.onServerConnectionState -= OnServerConnectionState;
            _networkManager.Unsubscribe<ConnectionSceneRequest>(OnServerConnectionSceneRequest, asServer: true);

            if (_networkManager.sceneModule != null)
            {
                _networkManager.sceneModule.onSceneLoaded -= OnSceneLoaded;
                _networkManager.sceneModule.onSceneUnloaded -= OnSceneUnloaded;
            }
        }

        // The sceneModule is created when the NetworkManager actually connects (server or client),
        // so OnEnable can run before it exists. Hook it up lazily.
        private void TrySubscribeSceneEvents()
        {
            var module = _networkManager.sceneModule;
            if (module == null) return;

            module.onSceneLoaded -= OnSceneLoaded;
            module.onSceneUnloaded -= OnSceneUnloaded;
            module.onSceneLoaded += OnSceneLoaded;
            module.onSceneUnloaded += OnSceneUnloaded;
        }

        public async UniTask<Scene?> AwaitLoadConnectionScene(string sceneName)
        {
            if (_loadedScenes.TryGetFirst(s => s.name == sceneName, out var scene))
                return scene;

            TrySubscribeSceneEvents();

            var completionSource = new UniTaskCompletionSource<Scene>();
            void OnLoaded(SceneID id, bool asServer)
            {
                if (_networkManager.sceneModule != null &&
                    _networkManager.sceneModule.TryGetSceneState(id, out var state) &&
                    state.scene.name == sceneName)
                {
                    completionSource.TrySetResult(state.scene);
                }
            }

            if (_networkManager.sceneModule != null)
                _networkManager.sceneModule.onSceneLoaded += OnLoaded;

            try
            {
                _networkManager.SendToServer(new ConnectionSceneRequest(sceneName, ConnectionSceneRequestOperation.Load));
                var result = await completionSource.Task.TimeoutWithoutException(TimeSpan.FromSeconds(4));
                return result.IsTimeout ? null : result.Result;
            }
            finally
            {
                if (_networkManager.sceneModule != null)
                    _networkManager.sceneModule.onSceneLoaded -= OnLoaded;
            }
        }

        public void UnloadConnectionScene(string sceneName)
        {
            if (_loadedScenes.All(s => s.name != sceneName))
                return;

            _networkManager.SendToServer(new ConnectionSceneRequest(sceneName, ConnectionSceneRequestOperation.Unload));
        }

        private void OnServerConnectionSceneRequest(PlayerID sender, ConnectionSceneRequest request, bool asServer)
        {
            if (!asServer) return;

            if (request.Operation == ConnectionSceneRequestOperation.Load)
                RequestLoadScene(request.SceneName);
            else
                RequestUnloadScene(request.SceneName);
        }

        private void RequestLoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            var module = _networkManager.sceneModule;
            if (module == null) return;

            if (_requestedSceneNames.Contains(sceneName))
                return;
            if (_loadedScenes.Any(s => s.name == sceneName))
                return;

            _requestedSceneNames.Add(sceneName);
            module.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        private void RequestUnloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            var module = _networkManager.sceneModule;
            if (module == null) return;

            module.UnloadSceneAsync(sceneName);
        }

        private void OnSceneLoaded(SceneID id, bool asServer)
        {
            var module = _networkManager.sceneModule;
            if (module == null || !module.TryGetSceneState(id, out var state))
                return;

            var scene = state.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            // Ignore PurrNet's bootstrap scenes (the NetworkManager's own scene + DontDestroyOnLoad).
            if (!IsManagedScene(scene.name))
                return;

            if (!_loadedScenes.Contains(scene))
                _loadedScenes.Add(scene);

            if (!GlobalScene.IsValid() && !string.IsNullOrEmpty(_GlobalScene) && scene.name == GetSceneName(_GlobalScene))
                GlobalScene = scene;

            if (_SetSceneActive && _OnlineScenes.Any(online => !string.IsNullOrEmpty(online) && scene.name == GetSceneName(online)))
                UnitySceneManager.SetActiveScene(scene);
        }

        private void OnSceneUnloaded(SceneID id, bool asServer)
        {
            var module = _networkManager.sceneModule;
            if (module == null || !module.TryGetSceneState(id, out var state))
                return;

            _loadedScenes.Remove(state.scene);
            _requestedSceneNames.Remove(state.scene.name);
            if (state.scene == GlobalScene)
                GlobalScene = default;
        }

        private bool IsManagedScene(string sceneName)
        {
            if (!string.IsNullOrEmpty(_GlobalScene) && sceneName == GetSceneName(_GlobalScene))
                return true;
            if (!string.IsNullOrEmpty(_ClientDefaultScene) && sceneName == GetSceneName(_ClientDefaultScene))
                return true;
            for (var i = 0; i < _OnlineScenes.Count; i++)
            {
                if (!string.IsNullOrEmpty(_OnlineScenes[i]) && sceneName == GetSceneName(_OnlineScenes[i]))
                    return true;
            }
            return false;
        }

        private void OnServerConnectionState(ConnectionState state)
        {
            TrySubscribeSceneEvents();

            if (state == ConnectionState.Connected)
            {
                if (!string.IsNullOrWhiteSpace(_GlobalScene))
                    RequestLoadScene(GetSceneName(_GlobalScene));

                foreach (var sceneRef in _OnlineScenes)
                {
                    if (!string.IsNullOrEmpty(sceneRef))
                        RequestLoadScene(GetSceneName(sceneRef));
                }

                if (!string.IsNullOrWhiteSpace(_ClientDefaultScene))
                    RequestLoadScene(GetSceneName(_ClientDefaultScene));
            }
            else if (state == ConnectionState.Disconnected)
            {
                UnloadOnlineScenes();
            }
        }

        private void OnClientConnectionState(ConnectionState state)
        {
            TrySubscribeSceneEvents();

            if (state == ConnectionState.Disconnected)
                UnloadOnlineScenes();
        }

        private void UnloadOnlineScenes()
        {
            // PurrNet's cleanup handles networked unloads when the connection drops, but we still
            // clear our local cache and best-effort unload any leftover Unity scenes.
            foreach (var scene in _loadedScenes)
            {
                if (scene.IsValid() && scene.isLoaded)
                    UnitySceneManager.UnloadSceneAsync(scene);
            }
            _loadedScenes.Clear();
            _requestedSceneNames.Clear();
            GlobalScene = default;
        }

        private string GetSceneName(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
