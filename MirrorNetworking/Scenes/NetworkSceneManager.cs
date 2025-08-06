// Author: František Holubec
// Created: 08.04.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.AssetTranslation;
using EDIVE.Core;
using EDIVE.Core.Restart;
using EDIVE.External.Signals;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.SceneManagement;
using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.Scenes
{
    // Simple single scene net manager
    // Todo: Allow multiple scenes, load/unload content based on player state, use some interest management from Mirror 
    public class NetworkSceneManager : ALoadableServiceBehaviour<NetworkSceneManager>
    {
        [ShowCreateNew]
        [EnhancedInlineEditor]
        [SerializeField]
        private ASceneDefinition _OnlineScene;

        public Signal ServerBeforeSceneChange { get; } = new();
        public Signal ServerSceneChanged { get; } = new();

        public Signal ClientBeforeSceneChange { get; } = new();
        public Signal ClientSceneChanged { get; } = new();

        private ASceneInstance _currentScene;
        private MasterNetworkManager _networkManager;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = AppCore.Services.Get<MasterNetworkManager>();
            _networkManager.ServerStarted.AddListener(OnServerStarted);
            _networkManager.ServerStopped.AddListener(OnServerStopped);
            _networkManager.ServerClientConnected.AddListener(OnServerClientConnected);

            _networkManager.ClientStarted.AddListener(OnClientStarted);
            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (AppCore.Services.TryGet(out _networkManager))
            {
                _networkManager.ServerStarted.RemoveListener(OnServerStarted);
                _networkManager.ServerStopped.RemoveListener(OnServerStopped);
                _networkManager.ServerClientConnected.RemoveListener(OnServerClientConnected);

                _networkManager.ClientStarted.RemoveListener(OnClientStarted);
            }
        }
        
        private void OnServerStarted()
        {
            if (_currentScene == null || _currentScene.BaseDefinition != _OnlineScene)
                ServerChangeScene(_OnlineScene);
        }

        private void OnServerStopped()
        {
            // ServerChangeScene(_OfflineScene);
        }

        private void ServerChangeScene(ASceneDefinition newSceneDef)
        {
            if (!newSceneDef.IsValid())
            {
                Debug.LogError($"Cannot change to invalid scene '{newSceneDef.UniqueID}'");
                return;
            }

            if (NetworkServer.isLoadingScene)
            {
                Debug.LogError($"Scene change is already in progress");
                return;
            }

            if (_currentScene != null && newSceneDef.Equals(_currentScene.BaseDefinition))
            {
                Debug.LogError($"Scene '{newSceneDef.UniqueID}' is already loaded");
                return;
            }

            // Throw error if called from client
            // Allow changing scene while stopping the server
            if (!NetworkServer.active)
            {
                Debug.LogError("ServerChangeScene can only be called on an active server.");
                return;
            }

            // Debug.Log($"ServerChangeScene {newSceneName}");
            NetworkServer.SetAllClientsNotReady();
            var newScene = newSceneDef.CreateInstance();

            UniTask.Void(async () =>
            {
                ServerBeforeSceneChange.Dispatch();
                NetworkServer.isLoadingScene = true;

                if (_networkManager.mode == NetworkManagerMode.Host)
                {
                    ClientBeforeSceneChange.Dispatch();
                    NetworkClient.isLoadingScene = true;
                }

                if (_currentScene != null)
                    await _currentScene.Unload();

                _currentScene = newScene;
                var newSceneLoadTask = newScene.Load();

                // ServerChangeScene can be called when stopping the server
                // when this happens the server is not active so does not need to tell clients about the change
                if (NetworkServer.active)
                {
                    // notify all clients about the new scene
                    NetworkServer.SendToAll(new LoadSceneMessage(newSceneDef.UniqueID));
                }

                NetworkManager.startPositionIndex = 0;
                NetworkManager.startPositions.Clear();

                await newSceneLoadTask;
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                NetworkServer.isLoadingScene = false;
                NetworkServer.SpawnObjects();

                ServerSceneChanged.Dispatch();

                if (_networkManager.mode == NetworkManagerMode.Host)
                {
                    NetworkClient.isLoadingScene = false;
                    FinalizeClientSceneLoad();
                    ClientSceneChanged.Dispatch();
                }
            });
        }

        private void OnServerClientConnected(NetworkConnectionToClient clientConnection)
        {
            // Send client the current scene
            clientConnection.Send(new LoadSceneMessage(_currentScene.BaseDefinition.UniqueID));
        }

        private void OnClientStarted()
        {
            // Unregister native scene handling and register our own
            NetworkClient.UnregisterHandler<SceneMessage>();
            NetworkClient.RegisterHandler<LoadSceneMessage>(OnClientSceneInternal);
        }

        private void OnClientSceneInternal(LoadSceneMessage msg)
        {
            // This needs to run for host client too. NetworkServer.active is checked there
            if (NetworkClient.isConnected && DefinitionTranslationUtils.TryGetDefinition(msg.SceneID, out ASceneDefinition definition))
            {
                ClientChangeScene(definition);
            }
        }

        private void ClientChangeScene(ASceneDefinition newSceneDef)
        {
            if (NetworkServer.active)
                return;

            if (!newSceneDef.IsValid())
            {
                Debug.LogError($"Cannot change to invalid scene '{newSceneDef.UniqueID}'");
                return;
            }

            if (NetworkClient.isLoadingScene)
            {
                Debug.LogError($"Scene change is already in progress for '{_currentScene.BaseDefinition.UniqueID}'");
                return;
            }

            if (newSceneDef.Equals(_currentScene.BaseDefinition))
            {
                Debug.LogError($"Scene '{newSceneDef.UniqueID}' is already loaded");
                return;
            }

            // Exit if server, since server is already doing the actual scene change, and we don't need to do it for the host client
            if (NetworkServer.active)
                return;

            var newScene = newSceneDef.CreateInstance();

            UniTask.Void(async () =>
            {
                ClientBeforeSceneChange.Dispatch();

                if (_currentScene != null)
                    await _currentScene.Unload();

                NetworkClient.isLoadingScene = true;
                var newSceneLoadTask = newScene.Load();

                await newSceneLoadTask;
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                NetworkClient.isLoadingScene = false;

                FinalizeClientSceneLoad();
                ClientSceneChanged.Dispatch();
            });
        }

        private void FinalizeClientSceneLoad()
        {
            if (NetworkClient.isConnected)
            {
                // always become ready.
                if (NetworkClient.connection.isAuthenticated && !NetworkClient.ready)
                    NetworkClient.Ready();

                // Only call AddPlayer for normal scene changes, not additive load/unload
                if (NetworkClient.connection.isAuthenticated && _networkManager.autoCreatePlayer && NetworkClient.localPlayer == null)
                {
                    // add player if existing one is null
                    NetworkClient.AddPlayer();
                }
            }
        }
        
        [ExecuteOnAppRestart(-100)]
        public static async UniTask OnAppRestart()
        {
            if (!AppCore.Services.TryGet<NetworkSceneManager>(out var sceneManager))
            {
                Debug.LogError("Cannot clear scenes, missing network scene manager!");
                return;
            }
            
            await sceneManager.UnloadAllScenes();
        }
        
        private async UniTask UnloadAllScenes()
        {
            await _currentScene.Unload();
        }
    }
}
