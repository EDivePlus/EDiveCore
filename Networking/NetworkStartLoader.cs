// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
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
                case NetworkRuntimeMode.Server:
                    networkManager.StartRuntime(NetworkRuntimeMode.Server);
                    break;

                case NetworkRuntimeMode.Host:
                    networkManager.StartRuntime(NetworkRuntimeMode.Host);
                    break;

                case NetworkRuntimeMode.Client:
                    networkManager.StartRuntime(NetworkRuntimeMode.Client);
                    break;
                
                case NetworkRuntimeMode.Offline:
                    break;
                
                default: 
                    throw new ArgumentOutOfRangeException();
            }
            
        }
    }
}
