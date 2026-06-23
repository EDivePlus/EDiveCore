// Author: František Holubec
// Created: 08.04.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.Networking.Scenes
{
    public class NetworkSceneManager : ALoadableServiceBehaviour<NetworkSceneManager>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SceneReference> _GlobalSceneReferences = new();

        [SerializeField]
        [HideInInspector]
        private List<string> _GlobalScenes;

        [SerializeField]
        private float _JoinTimeout = 5f;

        private NetworkManager _networkManager;

        private readonly Dictionary<SceneKey, SceneID> _serverLoadedScenes = new();
        private readonly Dictionary<SceneKey, UniTaskCompletionSource<SceneID>> _serverPendingLoads = new();
        private readonly HashSet<SceneKey> _clientJoinedScenes = new();

        public IReadOnlyCollection<SceneKey> JoinedScenes => _clientJoinedScenes;

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

            if (_networkManager.sceneModule == null) return;
            _networkManager.sceneModule.onSceneLoaded -= OnServerSceneLoaded;
#if ADDRESSABLES
            _networkManager.sceneModule.onAddressableSceneLoaded -= OnServerAddressableSceneLoaded;
#endif
        }

        public async UniTask<Scene?> AwaitJoinScene(SceneReference reference)
        {
            if (!reference.IsValid) return null;
            var key = reference.Key;
            if (!key.IsValid) return null;

            var module = _networkManager.sceneModule;
            var tcs = new UniTaskCompletionSource<Scene>();

            void OnDirectLoaded(SceneID id, bool asServer)
            {
                if (module.TryGetSceneState(id, out var state) &&
                    state.scene.IsValid() &&
                    state.scene.isLoaded &&
                    state.scene.name == key.Id)
                {
                    tcs.TrySetResult(state.scene);
                }
            }

#if ADDRESSABLES
            void OnAddressableLoaded(SceneID id, string guid, bool asServer)
            {
                if (guid != key.Id) return;
                if (module.TryGetSceneState(id, out var state) &&
                    state.scene.IsValid() &&
                    state.scene.isLoaded)
                {
                    tcs.TrySetResult(state.scene);
                }
            }
#endif

            if (key.IsAddressable)
            {
#if ADDRESSABLES
                module.onAddressableSceneLoaded += OnAddressableLoaded;
#endif
            }
            else
            {
                module.onSceneLoaded += OnDirectLoaded;
            }

            try
            {
                if (TryGetLoadedScene(key, out var existing))
                {
                    _clientJoinedScenes.Add(key);
                    return existing;
                }

                _networkManager.SendToServer(new ConnectionSceneRequest(key, ConnectionSceneRequestOperation.Join));

                var result = await tcs.Task.TimeoutWithoutException(TimeSpan.FromSeconds(_JoinTimeout));
                if (result.IsTimeout)
                {
                    Debug.LogWarning($"[NetworkSceneManager] Timed out waiting for scene '{key}' to load.", this);
                    return null;
                }

                _clientJoinedScenes.Add(key);
                return result.Result;
            }
            finally
            {
                if (key.IsAddressable)
                {
#if ADDRESSABLES
                    module.onAddressableSceneLoaded -= OnAddressableLoaded;
#endif
                }
                else
                {
                    module.onSceneLoaded -= OnDirectLoaded;
                }
            }
        }

        public void LeaveScene(SceneKey key)
        {
            if (!key.IsValid) return;
            if (_networkManager == null) return;

            _clientJoinedScenes.Remove(key);
            _networkManager.SendToServer(new ConnectionSceneRequest(key, ConnectionSceneRequestOperation.Leave));
        }

        private void OnConnectionSceneRequest(PlayerID sender, ConnectionSceneRequest request, bool asServer)
        {
            if (!asServer) return;

            switch (request.Operation)
            {
                case ConnectionSceneRequestOperation.Join:
                    ServerJoinScene(sender, request.Scene).Forget();
                    break;
                case ConnectionSceneRequestOperation.Leave:
                    ServerLeaveScene(sender, request.Scene);
                    break;
            }
        }

        private async UniTask ServerJoinScene(PlayerID player, SceneKey key)
        {
            try
            {
                var sceneId = await ServerEnsureSceneLoaded(key, isPublic: false);
                if (sceneId == null) return;

                if (_networkManager.TryGetModule<ScenePlayersModule>(true, out var scenePlayers))
                    scenePlayers.AddPlayerToScene(player, sceneId.Value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ServerLeaveScene(PlayerID player, SceneKey key)
        {
            if (!_serverLoadedScenes.TryGetValue(key, out var sceneId))
                return;

            if (!_networkManager.TryGetModule<ScenePlayersModule>(true, out var scenePlayers))
                return;

            scenePlayers.RemovePlayerFromScene(player, sceneId);

            if (IsGlobalScene(key))
                return;

            if (scenePlayers.TryGetPlayersAttachedToScene(sceneId, out var remaining) && remaining.Count == 0)
            {
                if (key.IsAddressable)
                {
#if ADDRESSABLES
                    _networkManager.sceneModule.UnloadAddressableSceneByGuid(key.Id);
#endif
                }
                else
                {
                    _networkManager.sceneModule.UnloadSceneAsync(sceneId);
                }
                _serverLoadedScenes.Remove(key);
            }
        }

        private async UniTask<SceneID?> ServerEnsureSceneLoaded(SceneKey key, bool isPublic)
        {
            if (_serverLoadedScenes.TryGetValue(key, out var existing))
                return existing;

            if (_serverPendingLoads.TryGetValue(key, out var pending))
            {
                try { return await pending.Task; }
                catch { return null; }
            }

            var tcs = new UniTaskCompletionSource<SceneID>();
            _serverPendingLoads[key] = tcs;

            var settings = new PurrSceneSettings
            {
                mode = LoadSceneMode.Additive,
                isPublic = isPublic
            };

            bool started;
            if (key.IsAddressable)
            {
#if ADDRESSABLES
                var loadHandle = _networkManager.sceneModule.LoadAddressableSceneAsync(key.Id, settings);
                started = loadHandle.IsValid();
#else
                started = false;
#endif
            }
            else
            {
                var op = _networkManager.sceneModule.LoadSceneAsync(key.Id, settings);
                started = op != null;
            }

            if (!started)
            {
                _serverPendingLoads.Remove(key);
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
                _clientJoinedScenes.Clear();
            }
        }

        private void HookServerSceneEvents()
        {
            var module = _networkManager.sceneModule;
            if (module == null) return;

            module.onSceneLoaded -= OnServerSceneLoaded;
            module.onSceneLoaded += OnServerSceneLoaded;
#if ADDRESSABLES
            module.onAddressableSceneLoaded -= OnServerAddressableSceneLoaded;
            module.onAddressableSceneLoaded += OnServerAddressableSceneLoaded;
#endif
        }

        private void LoadGlobalScenes()
        {
            if (_networkManager.sceneModule == null) return;

            foreach (var sceneRef in _GlobalSceneReferences)
            {
                if (!sceneRef.IsValid) continue;
                ServerEnsureSceneLoaded(sceneRef.Key, isPublic: true).Forget();
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

            ResolvePendingLoad(new SceneKey(state.scene.name, false), id);
        }

#if ADDRESSABLES
        private void OnServerAddressableSceneLoaded(SceneID id, string guid, bool asServer)
        {
            if (!asServer) return;
            ResolvePendingLoad(new SceneKey(guid, true), id);
        }
#endif

        private void ResolvePendingLoad(SceneKey key, SceneID id)
        {
            if (!_serverPendingLoads.TryGetValue(key, out var tcs))
                return;

            _serverLoadedScenes[key] = id;
            tcs.TrySetResult(id);
            _serverPendingLoads.Remove(key);
        }

        private bool TryGetLoadedScene(SceneKey key, out Scene scene)
        {
            scene = default;
            var module = _networkManager?.sceneModule;
            if (module == null) return false;

            if (key.IsAddressable)
            {
#if ADDRESSABLES
                if (module.TryGetSceneIdByAddressableGuid(key.Id, out var id) &&
                    module.TryGetSceneState(id, out var state) &&
                    state.scene.IsValid() && state.scene.isLoaded)
                {
                    scene = state.scene;
                    return true;
                }
#endif
                return false;
            }

            foreach (var state in module.sceneStates.Values)
            {
                var s = state.scene;
                if (s.name == key.Id && s.IsValid() && s.isLoaded)
                {
                    scene = s;
                    return true;
                }
            }
            return false;
        }

        private bool IsBootstrapScene(Scene scene)
        {
            if (scene.name == "DontDestroyOnLoad") return true;
            if (_networkManager != null && _networkManager.gameObject.scene == scene) return true;
            return false;
        }

        private bool IsGlobalScene(SceneKey key)
        {
            foreach (var g in _GlobalSceneReferences)
            {
                if (g.IsValid && g.Key.Equals(key))
                    return true;
            }
            return false;
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            if (_GlobalScenes == null || _GlobalScenes.Count == 0) return;
            if (_migrationQueued) return;
            _migrationQueued = true;
            UnityEditor.EditorApplication.delayCall += MigrateLegacyScenes;
#endif
        }

#if UNITY_EDITOR
        [NonSerialized]
        private bool _migrationQueued;

        private void MigrateLegacyScenes()
        {
            _migrationQueued = false;
            if (this == null) return;
            if (_GlobalScenes == null || _GlobalScenes.Count == 0) return;

            _GlobalSceneReferences ??= new List<SceneReference>();
            foreach (var path in _GlobalScenes)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path);
                if (asset == null)
                {
                    Debug.LogWarning($"[NetworkSceneManager] Could not migrate legacy global scene path '{path}'", this);
                    continue;
                }
                _GlobalSceneReferences.Add(SceneReference.FromSceneAsset(asset));
            }

            _GlobalScenes = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[NetworkSceneManager] Migrated legacy global scenes", this);
        }
#endif
    }
}
