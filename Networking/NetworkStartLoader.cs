// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using EDIVE.Networking.Utils;

namespace EDIVE.Networking
{
    [Serializable]
    public class NetworkStartLoader : ILoadable
    {
        public async UniTask Load(Action<float> progressCallback)
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            switch (NetworkUtils.RuntimeMode)
            {
                case NetworkRuntimeMode.Client:
                    var serverManager = await AppCore.Services.AwaitRegistered<NetworkServerManager>();
                    await serverManager.AutoConnectAsync();
                    break;

                case NetworkRuntimeMode.Server:
                case NetworkRuntimeMode.Host:
                case NetworkRuntimeMode.Offline:
                    networkManager.StartRuntime(NetworkUtils.RuntimeMode);
                    break;

                case NetworkRuntimeMode.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
