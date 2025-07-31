// Author: František Holubec
// Created: 15.06.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using LightReflectiveMirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement
{
    [RequireComponent(typeof(LightReflectiveMirrorTransport))]
    public class RelayInitializer : MonoBehaviour, ILoadable
    {
        [SerializeField]
        private RelayConfig _Config;

        [SerializeField]
        private float _ConnectTimeout = 5f;

        public UniTask Load(Action<float> progressCallback)
        {
            var transport = GetComponent<LightReflectiveMirrorTransport>();
            _Config.ApplyTo(transport);
            transport.ConnectToRelay();
            AwaitRelay(transport).Forget();
            return UniTask.CompletedTask;
        }

        private async UniTask AwaitRelay(LightReflectiveMirrorTransport transport)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(_ConnectTimeout));
            try
            {
                await UniTask.WaitUntil(transport.IsAuthenticated, PlayerLoopTiming.Update, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogError("LRM not available");
            }
        }
    }
}
