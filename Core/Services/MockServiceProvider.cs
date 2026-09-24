// Author: František Holubec
// Created: 02.04.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Core.Services
{
    public class MockServiceProvider : IServiceProvider
    {
        public static readonly MockServiceProvider INSTANCE = new();

        public void Register<T>(T service) where T : class, IService
        {
            PrintMockMessage(nameof(Register));
        }

        public bool TryRegister<T>(T service) where T : class, IService
        {
            PrintMockMessage(nameof(TryRegister));
            return false;
        }

        public bool Unregister<T>() where T : class, IService
        {
            PrintMockMessage(nameof(Unregister));
            return false;
        }

        public bool Unregister<T>(T service) where T : class, IService
        {
            PrintMockMessage(nameof(Unregister));
            return false;
        }

        public T Get<T>() where T : class, IService
        {
            PrintMockMessage(nameof(Get));
            return null;
        }

        public bool TryGet<T>(out T service) where T : class, IService
        {
            PrintMockMessage(nameof(TryGet));
            service = null;
            return false;
        }

        public bool IsRegistered<T>() where T : class, IService
        {
            PrintMockMessage(nameof(IsRegistered));
            return false;
        }

        public bool IsRegisteredWith<T>(object service) where T : class, IService
        {
            PrintMockMessage(nameof(IsRegisteredWith));
            return false;
        }

        public void SubscribeOnChangeWithInitial<T>(Action<T> handler) where T : class, IService
        {
            PrintMockMessage(nameof(IsRegisteredWith));
        }

        public void SubscribeOnChange<T>(Action<T> handler) where T : class, IService
        {
            PrintMockMessage(nameof(IsRegisteredWith));
        }

        public void UnsubscribeOnChange<T>(Action<T> handler) where T : class, IService
        {
            PrintMockMessage(nameof(IsRegisteredWith));
        }

        public IDisposable WhenRegistered<T>(Action<T> action) where T : class, IService
        {
            PrintMockMessage(nameof(WhenRegistered));
            return DisposableUtils.Empty;
        }

        public IDisposable WhenRegistered<T, T2>(Action<T, T2> action) where T : class, IService where T2 : class, IService
        {
            PrintMockMessage(nameof(WhenRegistered));
            return DisposableUtils.Empty;
        }

        public IDisposable WhenRegistered<T, T2, T3>(Action<T, T2, T3> action) where T : class, IService where T2 : class, IService where T3 : class, IService
        {
            PrintMockMessage(nameof(WhenRegistered));
            return DisposableUtils.Empty;
        }

        public UniTask<T> AwaitRegistered<T>(CancellationToken cancellationToken = default) where T : class, IService
        {
            PrintMockMessage(nameof(AwaitRegistered));
            return UniTask.Never<T>(CancellationToken.None);
        }

        public UniTask<(T, T2)> AwaitRegistered<T, T2>(CancellationToken cancellationToken = default) where T : class, IService where T2 : class, IService
        {
            PrintMockMessage(nameof(AwaitRegistered));
            return UniTask.Never<(T, T2)>(CancellationToken.None);
        }

        public UniTask<(T, T2, T3)> AwaitRegistered<T, T2, T3>(CancellationToken cancellationToken = default) where T : class, IService where T2 : class, IService where T3 : class, IService
        {
            PrintMockMessage(nameof(AwaitRegistered));
            return UniTask.Never<(T, T2, T3)>(CancellationToken.None);
        }

        private static void PrintMockMessage(string methodName)
        {
            Debug.Log($"[ServiceProvider] {methodName} will be ignored as Provider is not ready or application is quitting!");
        }
    }
}
