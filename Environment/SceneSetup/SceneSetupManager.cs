// Author: František Holubec
// Created: 27.08.2025

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Core.Services;
using EDIVE.External.Signals;
using EDIVE.NativeUtils;
using EDIVE.Networking.Scenes;
using EDIVE.Utils.Loading;
using EDIVE.XRTools.Controls;
using FishNet;
using FishNet.Managing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupManager : AServiceBehaviour<SceneSetupManager>
    {
        [SerializeField]
        private SceneSetupDefinition _DefaultSetup;
        
        public SceneSetupDefinition CurrentSetup { get; private set; }
        public Signal<SceneSetupDefinition> CurrentContextChanged { get; } = new();

        private NetworkManager _networkManager;
        private bool _switchInProgress;
        private readonly List<ASceneSpawnPlace> _spawnPlaces = new();
        private readonly List<SceneSetupController> _sceneControllers = new();

        protected void Awake()
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
                return;
            
            _networkManager.ClientManager.OnAuthenticated += OnClientAuthenticated;
        }

        protected void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.ClientManager.OnAuthenticated -= OnClientAuthenticated;
            }
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
        
        private void OnClientAuthenticated()
        {
            try
            {
                SetCurrentContext(_DefaultSetup);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
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

            if (AppCore.Services.TryGet<LoadingOverlayProvider>(out var overlay)) 
                await overlay.RequestOverlayAndWait(this);

            // Load the scenes

            Scene?[] loadedScenes = null;
            var defScenes = definition.Scenes.ToList();
            if (defScenes.Any() && AppCore.Services.TryGet<NetworkSceneManager>(out var networkSceneManager))
            {
                var scenesToUnload = networkSceneManager.LoadedConnectionScenes
                    .Where(loadedScene => defScenes.All(defScene => GetSceneName(defScene) != loadedScene.name))
                    .Select(loadedScene => loadedScene.name);

                foreach (var sceneToUnload in scenesToUnload)
                {
                    networkSceneManager.UnloadConnectionScene(sceneToUnload);
                }

                loadedScenes = await UniTask.WhenAll(defScenes.Select(defScene => networkSceneManager.AwaitLoadConnectionScene(GetSceneName(defScene))));
                if (loadedScenes.Any())
                {
                    if (definition.SetFirstSceneActive)
                    {
                        var firstScene = loadedScenes.FirstOrDefault(scene => scene != null && scene.Value.isLoaded);
                        if (firstScene != null)
                            UnitySceneManager.SetActiveScene(firstScene.Value);
                    }

                    await UniTask.Yield();
                }
            }

            // Apply visual
            foreach (var sceneController in _sceneControllers)
            {
                if (sceneController != null)
                    sceneController.ApplyDefinition(definition);
            }

            // Find spawn place and teleport
            if (_spawnPlaces.TryGetFirst(s => s != null && loadedScenes != null && loadedScenes.Contains(s.gameObject.scene), out var spawnPlace))
            {
                if (AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                {
                    var connection = InstanceFinder.ClientManager.Connection;
                    if (spawnPlace.TryGetLocation(connection, out var position, out var rotation))
                    {
                        controlsManager.RequestTeleport(position, rotation);
                    }
                }
            }
            await UniTask.Yield();
            if (AppCore.Services.TryGet(out overlay)) 
                overlay.ReleaseOverlay(this);
            
            CurrentSetup = definition;
            _switchInProgress = false;
            CurrentContextChanged.Dispatch(CurrentSetup);
        }
        
        private string GetSceneName(string fullPath)
        {
            return string.IsNullOrEmpty(fullPath) ? fullPath : Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
