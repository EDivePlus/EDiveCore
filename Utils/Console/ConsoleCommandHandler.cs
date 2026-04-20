using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EDIVE.Utils.Console
{
    public static class ConsoleCommandHandler
    {
        private static CancellationTokenSource _cts;
        private static readonly ConcurrentQueue<string> _commandQueue = new();

        public static event Action<string> OnLog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if !UNITY_EDITOR
            var inputThread = new Thread(ReadInput) { IsBackground = true };
            inputThread.Start();
#endif
            _cts = new CancellationTokenSource();
            ProcessCommands(_cts.Token).Forget();
            Application.quitting += () => _cts.Cancel();
        }

        public static void EnqueueCommand(string cmd)
        {
            _commandQueue.Enqueue(cmd);
        }

        private static void ReadInput()
        {
            while (!_cts.IsCancellationRequested)
            {
                var line = System.Console.ReadLine();
                if (line != null)
                    _commandQueue.Enqueue(line.Trim());
            }
        }

        private static async UniTaskVoid ProcessCommands(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                while (_commandQueue.TryDequeue(out var cmd))
                {
                    HandleCommand(cmd);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private static void Log(string msg)
        {
            System.Console.WriteLine(msg);
#if UNITY_EDITOR
            Debug.Log($"[Console] {msg}");
#endif
            OnLog?.Invoke(msg);
        }

        private static void HandleCommand(string cmd)
        {
            var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            switch (parts[0].ToLower())
            {
                case "status":
                    Log("Server is running.");
                    break;
                case "quit":
                    Log("Shutting down...");
                    Application.Quit();
                    break;
                default:
                    Log($"Unknown command: {parts[0]}");
                    break;
            }
        }
    }
}