// Author: Michal Petr
// Created: 11.03.2026

using System;

namespace EDIVE.NativeUtils
{
    public static class DisposableUtils
    {
        public static readonly IDisposable Empty = new EmptyDisposable();
        
        public static IDisposable Create(Action onDisposed)
        {
            return new AnonymousDisposable(onDisposed);
        }
    }
    
    internal sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
        
    internal sealed class AnonymousDisposable : IDisposable
    {
        private readonly Action _onDisposed;

        public AnonymousDisposable(Action onDisposed) => _onDisposed = onDisposed;

        public void Dispose() => _onDisposed?.Invoke();
    }
}
