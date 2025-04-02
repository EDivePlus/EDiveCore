// Author: František Holubec
// Created: 02.04.2025

using System;
using Cysharp.Threading.Tasks;

namespace EDIVE.Core.Services
{
    public interface IServiceProvider
    {
        public void Register<T>(T service) where T : class, IService;
        public bool TryRegister<T>(T service) where T : class, IService;

        public bool Unregister<T>() where T : class, IService;
        public bool Unregister<T>(T service) where T : class, IService;

        public T Get<T>() where T : class, IService;
        public bool TryGet<T>(out T service) where T : class, IService;

        public bool IsRegistered<T>() where T : class, IService;
        public bool IsRegisteredWith<T>(object service) where T : class, IService;
        public void WhenRegistered<T>(Action<T> action) where T : class, IService;
        public UniTask<T> AwaitRegistered<T>() where T : class, IService;
    }
}
