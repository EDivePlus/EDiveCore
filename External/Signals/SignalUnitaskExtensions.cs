// Author: František Holubec
// Created: 17.10.2025

#if UNITASK
using Cysharp.Threading.Tasks;

namespace EDIVE.External.Signals
{
    public static class SignalUnitaskExtensions
    {
        public static async UniTask Await(this Signal signal)
        {
            var tcs = new UniTaskCompletionSource();
            signal.AddOnceListener(() => tcs.TrySetResult());
            await tcs.Task;
        }
        
        public static async UniTask<T> Await<T>(this Signal<T> signal)
        {
            var tcs = new UniTaskCompletionSource<T>();
            signal.AddOnceListener(t => tcs.TrySetResult(t));
            return await tcs.Task;
        }
        
        public static async UniTask<(T, T2)> Await<T, T2>(this Signal<T, T2> signal)
        {
            var tcs = new UniTaskCompletionSource<(T, T2)>();
            signal.AddOnceListener((t, t2) => tcs.TrySetResult((t, t2)));
            return await tcs.Task;
        }
        
        public static async UniTask<(T, T2, T3)> Await<T, T2, T3>(this Signal<T, T2, T3> signal)
        {
            var tcs = new UniTaskCompletionSource<(T, T2, T3)>();
            signal.AddOnceListener((t, t2, t3) => tcs.TrySetResult((t, t2, t3)));
            return await tcs.Task;
        }
        
        public static async UniTask<(T, T2, T3, T4)> Await<T, T2, T3, T4>(this Signal<T, T2, T3, T4> signal)
        {
            var tcs = new UniTaskCompletionSource<(T, T2, T3, T4)>();
            signal.AddOnceListener((t, t2, t3, t4) => tcs.TrySetResult((t, t2, t3, t4)));
            return await tcs.Task;
        }
    }
}
#endif
