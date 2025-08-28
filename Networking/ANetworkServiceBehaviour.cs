// Author: František Holubec
// Created: 27.08.2025

using EDIVE.Core;
using EDIVE.Core.Services;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Networking
{
    public abstract class ANetworkServiceBehaviour<T> : NetworkBehaviour, IService
        where T : class, IService
    {
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            RegisterService();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            UnregisterService();
        }

        protected void RegisterService<TService>() where TService : class, IService
        {
            if (this is not TService targetType)
            {
                Debug.LogError($"Service '{GetType()}' cannot be registered as '{typeof(TService)}'", this);
                return;
            }
            AppCore.Services.Register(targetType);
        }

        protected void UnregisterService<TService>() where TService : class, IService
        {
            if (this is not TService)
            {
                Debug.LogError($"Service '{GetType()}' cannot be unregistered as '{typeof(TService)}'", this);
                return;
            }
            AppCore.Services.Unregister<TService>();
        }

        protected virtual void RegisterService() => RegisterService<T>();
        protected virtual void UnregisterService() => UnregisterService<T>();
    }
}
