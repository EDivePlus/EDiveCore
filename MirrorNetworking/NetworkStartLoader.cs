// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;

namespace EDIVE.MirrorNetworking
{
    [Serializable]
    public class NetworkStartLoader : ILoadable
    {
        public UniTask Load(Action<float> progressCallback)
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            if (NetworkUtils.RuntimeMode == NetworkRuntimeMode.Server)
            {
                networkManager.StartServer();
            }
            return UniTask.CompletedTask;
        }
    }
}
