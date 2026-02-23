// Author: František Holubec
// Created: 09.03.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.External.Promises;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Services
{
    public class ServiceProvider : IServiceProvider
    {
        [ShowInInspector]
        private readonly Dictionary<Type, IServiceWrapper> _services = new();
        
        private ServiceWrapper<T> GetServiceWrapper<T>() where T : class, IService
        {
            var type = typeof(T);
            if(_services.TryGetValue(type, out var wrapper)) 
                return wrapper as ServiceWrapper<T>;

            var tWrapper = new ServiceWrapper<T>();
            _services.Add(type, tWrapper);
            return tWrapper;
        }
        
        private bool TryGetServiceWrapper<T>(out ServiceWrapper<T> resultWrapper) where T : class, IService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var wrapper) && wrapper is ServiceWrapper<T> tWrapper)
            {
                resultWrapper = tWrapper;
                return true;
            }
            
            resultWrapper = null;
            return false;
        }

        public void Register<T>(T service) where T : class, IService
        {
            var wrapper = GetServiceWrapper<T>();
            wrapper.SetService(service);
            DebugLite.Log($"[ServiceProvider] '{typeof(T).Name}' registered");
        }

        public bool TryRegister<T>(T service) where T : class, IService
        {
            if (TryGetServiceWrapper<T>(out var wrapper) && wrapper.HasService)
                return false;
            
            Register(service);
            return true;
        }

        public bool Unregister<T>() where T : class, IService
        {
            if (!TryGetServiceWrapper<T>(out var wrapper)) 
                return false;

            var result = wrapper.ClearService();
            if (result)
                DebugLite.Log($"[ServiceProvider] '{typeof(T).Name}' unregistered");
            return result;
        }

        public bool Unregister<T>(T service) where T : class, IService
        {
            var result = TryGetServiceWrapper<T>(out var wrapper) && wrapper.ClearService();
            if (result)
                DebugLite.Log($"[ServiceProvider] '{typeof(T).Name}' unregistered");
            return result;
        }
        
        public T Get<T>() where T : class, IService
        {
            return TryGet<T>(out var result) ? result : null;
        }

        public bool TryGet<T>(out T service) where T : class, IService
        {
            if (TryGetServiceWrapper<T>(out var wrapper) && wrapper.HasService)
            {
                service = wrapper.Service;
                return true;
            }
            
#if UNITY_EDITOR
            foreach (var registeredService in _services.Keys)
            {
                if (registeredService.IsAssignableFrom(typeof(T)))
                {
                    service = _services[registeredService].BaseService as T;
                    if (service == null) continue;
                    Debug.LogError($"[ServiceProvider] Attempt to get service with child type. Call '{registeredService.Name}' instead of '{service.GetType().Name}'!");
                    return true;
                }

                if (typeof(T).IsAssignableFrom(registeredService))
                {
                    service = _services[registeredService].BaseService as T;
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
            return TryGetServiceWrapper<T>(out var wrapper) && wrapper.HasService;
        }

        public bool IsRegisteredWith<T>(object service) where T : class, IService
        {
            return TryGetServiceWrapper<T>(out var wrapper) && ReferenceEquals(wrapper.Service, service);
        }

        public void SubscribeOnChangeWithInitial<T>(Action<T> handler) where T : class, IService
        {
            var wrapper = GetServiceWrapper<T>();
            if (wrapper.HasService) 
                handler.Invoke(wrapper.Service);
            wrapper.ServiceChanged += handler;
        }
        
        public void SubscribeOnChange<T>(Action<T> handler) where T : class, IService
        {
            var wrapper = GetServiceWrapper<T>();
            wrapper.ServiceChanged += handler;
        }
        
        public void UnsubscribeOnChange<T>(Action<T> handler) where T : class, IService
        {
            if (TryGetServiceWrapper<T>(out var wrapper)) 
                wrapper.ServiceChanged -= handler;
        }

        public void WhenRegistered<T>(Action<T> action) where T : class, IService
        {
            var wrapper = GetServiceWrapper<T>();
            if (wrapper.HasService)
            {
               action.Invoke(wrapper.Service);
               return;
            }
            wrapper.Promise.Then(action);
        }
        
        public UniTask<T> AwaitRegistered<T>() where T : class, IService
        {
            var source = new UniTaskCompletionSource<T>();
            WhenRegistered<T>(service => source.TrySetResult(service));
            return source.Task;
        }
        
        public async UniTask<(T, T2)> AwaitRegistered<T, T2>() 
            where T : class, IService
            where T2 : class, IService
        {
            var t = await AwaitRegistered<T>();
            var t2 = await AwaitRegistered<T2>();
            return (t, t2);
        }
        
        public async UniTask<(T, T2, T3)> AwaitRegistered<T, T2, T3>() 
            where T : class, IService
            where T2 : class, IService
            where T3 : class, IService
        {
            var t = await AwaitRegistered<T>();
            var t2 = await AwaitRegistered<T2>();
            var t3 = await AwaitRegistered<T3>();
            return (t, t2, t3);
        }
        
        public void WhenRegistered<T, T2>(Action<T, T2> action)
            where T : class, IService
            where T2 : class, IService
        {
            AwaitRegistered<T, T2>().ContinueWith(r => action?.Invoke(r.Item1, r.Item2));
        }
        
        public void WhenRegistered<T, T2, T3>(Action<T, T2, T3> action)
            where T : class, IService
            where T2 : class, IService
            where T3 : class, IService
        {
            AwaitRegistered<T, T2, T3>().ContinueWith(r => action?.Invoke(r.Item1, r.Item2, r.Item3));
        }
        
        private interface IServiceWrapper
        { 
            IService BaseService { get; } 
        }

        private class ServiceWrapper<T> : IServiceWrapper where T : class, IService
        {
            public T Service { get; private set; }
            public IService BaseService => Service;
            public bool HasService => Service != null;
            
            public Promise<T> Promise => _promise ??= new Promise<T>();
            private Promise<T> _promise;
            
            public event Action<T> ServiceChanged;
            
            public void SetService(IService service)
            {
                if (service is not T && service != null)
                    return;
                Service = (T)service;
                ServiceChanged?.Invoke(Service);
                if (Service != null) 
                    _promise?.Dispatch(Service);
                _promise = null;
            }

            public bool ClearService()
            {
                if (Service == null) 
                    return false;
                
                Service = null;
                _promise = null;
                ServiceChanged?.Invoke(null);
                return true;
            }
        }
    }
}
