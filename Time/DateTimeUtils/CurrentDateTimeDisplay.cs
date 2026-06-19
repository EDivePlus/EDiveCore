// Author: František Holubec
// Created: 13.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Time.DateTimeUtils
{
    public class CurrentDateTimeDisplay : DateTimeDisplay
    {
        [SerializeField]
        [MinValue(0.01f)]
        private float _UpdateInterval = 1f;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            StartUpdating();
        }

        private void OnDisable()
        {
            StopUpdating();
        }

        private void StartUpdating()
        {
            StopUpdating();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            UpdateLoopAsync(_cts.Token).Forget();
        }

        private void StopUpdating()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTaskVoid UpdateLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                SetDateTime(DateTime.Now);
                await UniTask.Delay(TimeSpan.FromSeconds(_UpdateInterval), true, PlayerLoopTiming.Update, token);
            }
        }
    }
}
