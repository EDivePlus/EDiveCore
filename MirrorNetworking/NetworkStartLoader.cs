// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.MirrorNetworking
{
    [Serializable]
    public class NetworkStartLoader : ILoadable
    {
        public UniTask Load(Action<float> progressCallback)
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            var sceneManager = AppCore.Services.Get<NetSceneManager>();
            switch (NetworkUtils.RuntimeMode)
            {
                case NetworkRuntimeMode.Server:
                    networkManager.StartServer();
                    break;

                case NetworkRuntimeMode.Host:
                    networkManager.StartHost();
                    break;

                case NetworkRuntimeMode.Client:
                    sceneManager.LoadOfflineScene();
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
            return UniTask.CompletedTask;
        }
    }
}
