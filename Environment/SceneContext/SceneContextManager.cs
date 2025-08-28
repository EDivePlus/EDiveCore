// Author: František Holubec
// Created: 27.08.2025

using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Environment.Lighting;
using EDIVE.External.Signals;
using EDIVE.Networking;
using EDIVE.Networking.Scenes;
using EDIVE.XRTools.Controls;
using FishNet;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Environment.SceneContext
{
    public class SceneContextManager : ANetworkServiceBehaviour<SceneContextManager>
    {
        [SerializeField]
        private SceneContextDefinition _DefaultContext;
        
        public SceneContextDefinition CurrentContext { get; private set; }
        public Signal<SceneContextDefinition> CurrentContextChanged { get; } = new();

        private bool _switchInProgress;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetCurrentContext(_DefaultContext);
        }
        
        [Button]
        public void SetCurrentContext(SceneContextDefinition definition)
        {
            SetCurrentContextAsync(definition).Forget();
        }

        public async UniTask SetCurrentContextAsync(SceneContextDefinition definition)
        {
            if (_switchInProgress || definition == null || definition == CurrentContext)
                return;

            _switchInProgress = true;
            var defScenes = definition.Scenes.ToList();
            if (defScenes.Any() && AppCore.Services.TryGet<NetworkSceneManager>(out var networkSceneManager))
            {
                var scenesToUnload = networkSceneManager.LoadedScenes
                    .Where(loadedScene => defScenes.All(defScene => GetSceneName(defScene) != loadedScene.name))
                    .Select(loadedScene => loadedScene.name);
                
                foreach (var sceneToUnload in scenesToUnload)
                {
                    networkSceneManager.UnloadConnectionScene(sceneToUnload);
                }
                
                var loadedScenes = await UniTask.WhenAll(defScenes.Select(defScene => networkSceneManager.AwaitLoadConnectionScene(GetSceneName(defScene))));
                if (loadedScenes.Any(scene => scene == null))
                    return;
            }
            
            if (definition.Lighting != null && AppCore.Services.TryGet<LightingManager>(out var lightingManager))
                lightingManager.SetLighting(definition.Lighting);
            
            if (definition.SpawnPlace != null && AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
            {
                var connection = InstanceFinder.ClientManager.Connection;
                if (definition.SpawnPlace.TryGetLocation(connection, out var position, out var rotation))
                {
                    controlsManager.RequestTeleport(position, rotation);
                }
            }

            CurrentContext = definition;
            _switchInProgress = false;
            CurrentContextChanged.Dispatch(CurrentContext);
        }
        
        private string GetSceneName(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }
    }
}
