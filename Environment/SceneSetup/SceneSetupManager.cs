// Author: František Holubec
// Created: 27.08.2025

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.External.Signals;
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
        public Signal<SceneSetupDefinition> CurrentContextChanged { get; } = new();
        
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
            // TODO - too early ?
            SetCurrentContextAsync(_DefaultSetup).Forget();
        }

        [Button]
        public void SetCurrentContext(SceneSetupDefinition definition)
        {
            SetCurrentContextAsync(definition).Forget();
        }

        public async UniTask SetCurrentContextAsync(SceneSetupDefinition definition)
        {
            if (_switchInProgress || definition == null)
                return;

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
                CurrentContextChanged.Dispatch(CurrentSetup);
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
            }
        }

        private async UniTask<List<Scene>> SwitchScenes(SceneSetupDefinition definition)
        {
            var loaded = new List<Scene>();
            if (!AppCore.Services.TryGet<NetworkSceneManager>(out var networkSceneManager))
                return loaded;

            var targetNames = new HashSet<string>(definition.Scenes.Select(GetSceneName));
            
            var scenesToLeave = networkSceneManager.LoadedLocalScenes
                .Where(s => !targetNames.Contains(s.name))
                .Select(s => s.name)
                .ToList();

            foreach (var sceneToLeave in scenesToLeave)
                networkSceneManager.LeaveScene(sceneToLeave);
            
            var joinTasks = targetNames.Select(networkSceneManager.AwaitJoinScene).ToArray();
            var joined = await UniTask.WhenAll(joinTasks);

            foreach (var scene in joined)
            {
                if (scene.HasValue && scene.Value.IsValid() && scene.Value.isLoaded)
                    loaded.Add(scene.Value);
            }

            if (definition.SetFirstSceneActive && loaded.Count > 0)
                UnitySceneManager.SetActiveScene(loaded[0]);

            return loaded;
        }

        private void TeleportToSpawn(SceneSetupDefinition definition, List<Scene> loadedScenes)
        {
            if (loadedScenes.Count == 0) return;
            if (!AppCore.Services.TryGet<ControlsManager>(out var controlsManager)) return;

            if (!_spawnPlaces.TryGetFirst(s => s != null && s.CheckAvailable(definition) && loadedScenes.Contains(s.gameObject.scene), out var spawnPlace))
                return;

            var localPlayer = NetworkManager.main.localPlayer;
            if (spawnPlace.TryGetLocation(localPlayer, out var position, out var rotation))
                controlsManager.RequestTeleport(position, rotation);
        }

        private static string GetSceneName(string fullPath)
        {
            return string.IsNullOrEmpty(fullPath) ? fullPath : Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
