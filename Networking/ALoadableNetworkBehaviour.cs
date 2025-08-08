// Author: František Holubec
// Created: 08.08.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Core.Services;
using FishNet.Object;
using UnityEngine;

namespace EDIVE.Networking
{
    public abstract class ALoadableNetworkServiceBehaviour<T> : NetworkBehaviour, IService, ILoadable, IDependencyOwner
        where T : class, IService
    {
        protected virtual bool RegisterServiceOnLoad => true;
        
        public async UniTask Load(Action<float> progressCallback)
        {
            await LoadRoutine(progressCallback);
            if (RegisterServiceOnLoad)
            {
                RegisterService();
            }
        }

        protected virtual void OnDestroy()
        {
            UnregisterService();
        }

        protected abstract UniTask LoadRoutine(Action<float> progressCallback);

        public IEnumerable<Type> GetDependencies()
        {
            var dependencies = new HashSet<Type>();
            PopulateDependencies(dependencies);
            return dependencies;
        }

        protected virtual void PopulateDependencies(HashSet<Type> dependencies)
        {
            
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
