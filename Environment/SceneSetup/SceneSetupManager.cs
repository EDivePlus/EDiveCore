// Author: František Holubec
// Created: 27.08.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.NativeUtils;
using EDIVE.Input.Controls;
using EDIVE.Networking;
using EDIVE.Networking.Scenes;
using EDIVE.Utils.Loading;
using PurrNet;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupManager : ALoadableServiceBehaviour<SceneSetupManager>
    {
        [SerializeField]
        private SceneSetupDefinition _DefaultSetup;

        public SceneSetupDefinition CurrentSetup { get; private set; }
        public event Action<SceneSetupDefinition> CurrentContextChanged;
        
        private bool _switchInProgress;
        private readonly List<ASceneSpawnPlace> _spawnPlaces = new();
        private readonly List<SceneSetupController> _sceneControllers = new();

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            NetworkManager.main.onLocalPlayerReceivedID += OnClientAuthenticated;
            return UniTask.CompletedTask;
        }
        
        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            dependencies.Add(typeof(MasterNetworkManager));
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkManager.main == null) return;
            NetworkManager.main.onLocalPlayerReceivedID -= OnClientAuthenticated;
        }

        public void RegisterSceneController(SceneSetupController sceneController)
        {
            if (!_sceneControllers.Contains(sceneController))
                _sceneControllers.Add(sceneController);
        }

        public void UnregisterSceneController(SceneSetupController sceneController)
        {
            _sceneControllers.Remove(sceneController);
        }

        public void RegisterSpawnPlace(ASceneSpawnPlace spawnPlace)
        {
            if (!_spawnPlaces.Contains(spawnPlace))
                _spawnPlaces.Add(spawnPlace);
        }

        public void UnregisterSpawnPlace(ASceneSpawnPlace spawnPlace)
        {
            _spawnPlaces.Remove(spawnPlace);
        }

        private void OnClientAuthenticated(PlayerID player)
        {
            SetCurrentContextAsync(_DefaultSetup).Forget();
        }

        [Button]
        public void SetCurrentContext(SceneSetupDefinition definition)
        {
            SetCurrentContextAsync(definition).Forget();
        }

        public async UniTask SetCurrentContextAsync(SceneSetupDefinition definition)
        {
            Debug.Log($"[SceneSetupManager] Requesting scene setup change to {definition?.name ?? "<null>"}");
            if (definition == null)
            {
                Debug.LogWarning("[SceneSetupManager] SetCurrentContextAsync aborted: definition is null", this);
                return;
            }
            if (_switchInProgress)
            {
                Debug.LogWarning($"[SceneSetupManager] SetCurrentContextAsync aborted: switch already in progress (requested={definition.name})", this);
                return;
            }

            _switchInProgress = true;
            LoadingOverlayProvider overlay = null;
            try
            {
                if (AppCore.Services.TryGet(out overlay))
                    await overlay.RequestOverlayAndWait(this);

                var loadedScenes = await SwitchScenes(definition);

                foreach (var sceneController in _sceneControllers)
                {
                    if (sceneController != null)
                        sceneController.ApplyDefinition(definition);
                }

                TeleportToSpawn(definition, loadedScenes);

                CurrentSetup = definition;
                CurrentContextChanged?.Invoke(CurrentSetup);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (overlay != null)
                    overlay.ReleaseOverlay(this);
                _switchInProgress = false;
                
                Debug.Log($"[SceneSetupManager] Scene setup change to {definition.name} completed");
            }
        }

        private async UniTask<List<Scene>> SwitchScenes(SceneSetupDefinition definition)
        {
            var loaded = new List<Scene>();
            if (!AppCore.Services.TryGet<NetworkSceneManager>(out var networkSceneManager))
            {
                Debug.LogError("[SceneSetupManager] SwitchScenes aborted: NetworkSceneManager service is not registered", this);
                return loaded;
            }

            var targets = definition.Scenes.ToList();
            var targetKeys = new HashSet<SceneKey>(targets.Select(r => r.Key));
            Debug.Log($"[SceneSetupManager] SwitchScenes targets=[{string.Join(", ", targetKeys)}]", this);

            var scenesToLeave = networkSceneManager.JoinedScenes
                .Where(k => !targetKeys.Contains(k))
                .ToList();

            foreach (var sceneToLeave in scenesToLeave)
                networkSceneManager.LeaveScene(sceneToLeave);

            var joinTasks = targets.Select(networkSceneManager.AwaitJoinScene).ToArray();
            var joined = await UniTask.WhenAll(joinTasks);

            for (var i = 0; i < joined.Length; i++)
            {
                var scene = joined[i];
                if (scene.HasValue && scene.Value.IsValid() && scene.Value.isLoaded)
                    loaded.Add(scene.Value);
                else
                    Debug.LogWarning($"[SceneSetupManager] Scene '{targets[i].Key}' failed to join (null/invalid/not loaded)", this);
            }

            if (definition.SetFirstSceneActive && loaded.Count > 0)
                UnitySceneManager.SetActiveScene(loaded[0]);

            Debug.Log($"[SceneSetupManager] SwitchScenes finished: {loaded.Count}/{targetKeys.Count} scenes loaded", this);
            return loaded;
        }

        private void TeleportToSpawn(SceneSetupDefinition definition, List<Scene> loadedScenes)
        {
            if (loadedScenes.Count == 0)
            {
                Debug.LogWarning($"[SceneSetupManager] TeleportToSpawn skipped: no scenes loaded for setup '{definition.name}'", this);
                return;
            }
            if (!AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
            {
                Debug.LogWarning("[SceneSetupManager] TeleportToSpawn skipped: ControlsManager service is not registered", this);
                return;
            }

            if (!_spawnPlaces.TryGetFirst(s => s != null && s.CheckAvailable(definition) && loadedScenes.Contains(s.gameObject.scene), out var spawnPlace))
            {
                Debug.LogWarning($"[SceneSetupManager] TeleportToSpawn skipped: no spawn place matched setup '{definition.name}' across {_spawnPlaces.Count} registered places", this);
                return;
            }

            var localPlayer = NetworkManager.main.localPlayer;
            if (spawnPlace.TryGetLocation(localPlayer, out var position, out var rotation))
                controlsManager.RequestTeleport(position, rotation);
            else
                Debug.LogWarning($"[SceneSetupManager] TeleportToSpawn: spawn place '{spawnPlace.name}' did not provide a location for player {localPlayer}", this);
        }
    }
}
