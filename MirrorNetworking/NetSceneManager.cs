// Author: František Holubec
// Created: 08.04.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Core.Services;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.SceneManagement;
using UnityEngine;

namespace EDIVE.MirrorNetworking
{
    // Simple single scene net manager
    // Todo: Allow multiple scenes, load/unload content based on player state, use some interest management from Mirror 
    public class NetSceneManager : ALoadableServiceBehaviour<NetSceneManager>
    {
        [ShowCreateNew]
        [EnhancedInlineEditor]
        [SerializeField]
        private ASceneDefinition _OfflineScene;

        [ShowCreateNew]
        [EnhancedInlineEditor]
        [SerializeField]
        private ASceneDefinition _OnlineScene;

        private MasterNetworkManager _networkManager;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = AppCore.Services.Get<MasterNetworkManager>();
            _networkManager.ServerStarted.AddListener(OnServerStarted);
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
            }
        }
        
        private void OnServerStarted()
        {
            throw new NotImplementedException();
        }
    }
}
