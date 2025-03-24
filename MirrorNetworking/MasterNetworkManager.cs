// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using Mirror;

namespace EDIVE.MirrorNetworking
{
    public class MasterNetworkManager : NetworkManager, IService, ILoadable
    {
        public override void Start()
        {
            // Nothing, we will start the server or client in the load finalizer
        }

        public UniTask Load(Action<float> progressCallback)
        {
            AppCore.Services.Register(this);
            return UniTask.CompletedTask;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AppCore.Services.Unregister(this);
        }
    }
}
