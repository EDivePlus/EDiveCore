// Author: Michal Petr
// Created: 20.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using PurrNet;
using R3;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    public class NetworkStatisticsDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _PingText;
        
        [SerializeField]
        private TMP_Text _JitterText;
        
        [SerializeField]
        private TMP_Text _PacketLossText;
        
        [SerializeField]
        private TMP_Text _UploadText;
        
        [SerializeField]
        private TMP_Text _DownloadText;
        
        private StatisticsManager _statisticsManager;
        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            if (!AppCore.Services.TryGet(out MasterNetworkManager networkManager) || networkManager.StatisticsManager == null)
                return;
            
            _statisticsManager = networkManager.StatisticsManager;
            
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            
            if (_PingText)
            {
                Observable.Interval(TimeSpan.FromSeconds(_statisticsManager.checkInterval))
                    .Select(_ => _statisticsManager.ping)
                    .DistinctUntilChanged()
                    .Subscribe(ping => _PingText.text = $"{ping}ms")
                    .AddTo(_cts.Token);
            }

            if (_JitterText)
            {
                Observable.Interval(TimeSpan.FromSeconds(_statisticsManager.checkInterval))
                    .Select(_ => _statisticsManager.jitter)
                    .DistinctUntilChanged()
                    .Subscribe(jitter => _JitterText.text = $"{jitter}ms")
                    .AddTo(_cts.Token);
            }
            
            if (_PacketLossText)
            {
                Observable.Interval(TimeSpan.FromSeconds(_statisticsManager.checkInterval))
                    .Select(_ => _statisticsManager.packetLoss)
                    .DistinctUntilChanged()
                    .Subscribe(packetLoss => _PacketLossText.text = $"{packetLoss}%")
                    .AddTo(_cts.Token);
            }
            
            if (_UploadText)
            {
                Observable.Interval(TimeSpan.FromSeconds(_statisticsManager.checkInterval))
                    .Select(_ => _statisticsManager.upload)
                    .DistinctUntilChanged()
                    .Subscribe(upload => _UploadText.text = $"{upload:0.##}KB/s")
                    .AddTo(_cts.Token);
            }
            
            if (_DownloadText)
            {
                Observable.Interval(TimeSpan.FromSeconds(_statisticsManager.checkInterval))
                    .Select(_ => _statisticsManager.download)
                    .DistinctUntilChanged()
                    .Subscribe(download => _DownloadText.text = $"{download:0.##}KB/s")
                    .AddTo(_cts.Token);
            }
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
