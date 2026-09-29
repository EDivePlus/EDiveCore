// Author: František Holubec
// Created: 29.09.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EDIVE.UniTaskUtils
{
    public sealed class UniTaskPromise<T>
    {
        private readonly UniTaskCompletionSource<T> _source = new();

        public UniTaskStatus Status => _source.UnsafeGetStatus();
        public bool IsCompleted => Status.IsCompleted();

        public bool Resolve(T value) => _source.TrySetResult(value);
        public bool Reject(Exception e) => _source.TrySetException(e);
        public bool Cancel() => _source.TrySetCanceled();

        public UniTask<T> Await(CancellationToken ct = default) => ct.CanBeCanceled ? _source.Task.AttachExternalCancellation(ct) : _source.Task;
    }
}
