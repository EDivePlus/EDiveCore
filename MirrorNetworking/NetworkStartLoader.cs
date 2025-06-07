// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.MirrorNetworking.Scenes;
using EDIVE.MirrorNetworking.Utils;

namespace EDIVE.MirrorNetworking
{
    [Serializable]
    public class NetworkStartLoader : ILoadable
    {
        public async UniTask Load(Action<float> progressCallback)
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            var sceneManager = AppCore.Services.Get<NetworkSceneManager>();
            switch (NetworkUtils.RuntimeMode)
            {
                case NetworkRuntimeMode.Server:
                    networkManager.StartServer();
                    await sceneManager.ServerSceneChanged.Await();
                    break;

                case NetworkRuntimeMode.Host:
                    networkManager.StartHost();
                    await sceneManager.ClientSceneChanged.Await();
                    break;

                case NetworkRuntimeMode.Client:
                    await sceneManager.LoadOfflineScene();
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
