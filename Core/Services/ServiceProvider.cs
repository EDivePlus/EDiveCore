// Author: František Holubec
// Created: 09.03.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.External.Promises;
using UnityEngine;

namespace EDIVE.Core
{
    public class ServiceProvider
    {
        private readonly Dictionary<Type, IService> _services = new();
        private readonly Dictionary<Type, PromiseCaster> _promises = new();
        
        public void Register<T>(T service) where T : class, IService
        {
            var type = typeof(T);
            _services[type] = service;
            DebugLite.Log($"[ServiceProvider] '{type.Name}' registered");
       
            if (_promises == null || !_promises.TryGetValue(type, out var promiseCaster))
                return;
            
            promiseCaster.Dispatch(service);
            _promises.Remove(type);
        }

        public bool TryRegister<T>(T service) where T : class, IService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
                return false;
            
            Register(service);
            return true;
        }

        public bool Unregister<T>() where T : class, IService
        {
            var type = typeof(T);
            var result = _services.Remove(type);
            DebugLite.Log($"[ServiceProvider] '{type.Name}' unregistered");
            return result;
        }

        public bool Unregister<T>(T service) where T : class, IService
        {
            if (TryGet<T>(out var registeredService) && ReferenceEquals(registeredService, service))
                return Unregister<T>();
            return false;
        }
        
        public T Get<T>() where T : class, IService
        {
            return TryGet<T>(out var result) ? result : null;
        }

        public bool TryGet<T>(out T service) where T : class, IService
        {
            if (_services.TryGetValue(typeof(T), out var gotService) && gotService is T targetService)
            {
                service = targetService;
                return true;
            }
#if UNITY_EDITOR
            foreach (var registeredService in _services.Keys)
            {
                if (registeredService.IsAssignableFrom(typeof(T)))
                {
                    service = _services[registeredService] as T;
                    if (service == null) continue;
                    Debug.LogError($"[ServiceProvider] Attempt to get service with child type. Call '{registeredService.Name}' instead of '{service.GetType().Name}'!");
                    return true;
                }

                if (typeof(T).IsAssignableFrom(registeredService))
                {
                    service = _services[registeredService] as T;
                    if (service == null) continue;
                    Debug.LogError($"[ServiceProvider] Attempt to get service with parent type. Call '{registeredService.Name}' instead of '{service.GetType().Name}'!");
                    return true;
                }
            }
#endif
            service = null;
            return false;
        }

        public bool IsRegistered<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }

        public bool IsRegisteredWith<T>(object service) where T : class, IService
        {
            return _services.TryGetValue(typeof(T), out var registeredService) && ReferenceEquals(registeredService, service);
        }

        public void WhenRegistered<T>(Action<T> action) where T : class, IService
        {
            if (TryGet<T>(out var service))
            {
               action.Invoke(service);
               return;
            }

            var type = typeof(T);
            if (!_promises.TryGetValue(type, out var basePromiseCaster) || basePromiseCaster is not PromiseCaster<T> promiseCaster)
            {
                promiseCaster = new PromiseCaster<T>();
                _promises[type] = promiseCaster;
            } 
            promiseCaster.TargetPromise.Then(action);
        }
        
        public UniTask<T> Await<T>() where T : class, IService
        {
            var source = new UniTaskCompletionSource<T>();
            WhenRegistered<T>(service => source.TrySetResult(service));
            return source.Task;
        }
        
        private abstract class PromiseCaster
        {
            public abstract bool Dispatch(object service);
        }

        private class PromiseCaster<T> : PromiseCaster where T : class, IService
        {
            public Promise<T> TargetPromise { get; } = new();

            public override bool Dispatch(object service)
            {
                if (service is not T targetService) 
                    return false;
                
                TargetPromise.Dispatch(targetService);
                return true;
            }
        }
    }
}
