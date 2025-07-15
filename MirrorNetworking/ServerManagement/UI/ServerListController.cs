// Author: František Holubec
// Created: 13.06.2025

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.MirrorNetworking.Utils;
using EnhancedUI.EnhancedScroller;
using LightReflectiveMirror;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MirrorNetworking.ServerManagement.UI
{
    public class ServerListController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [SerializeField]
        public EnhancedScroller _Scroller;

        [SerializeField]
        private ServerListElementDisplay _ElementDisplayPrefab;

        [SerializeField]
        private Button _RefreshButton;

        private readonly List<ServerRecord> _currentServers = new();
        private NetworkServerManager _serverManager;
        
        private CancellationTokenSource _cancellationTokenSource;

        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<NetworkServerManager>(Initialize);
        }

        private void Initialize(NetworkServerManager manager)
        {
            _serverManager = manager;
            _Scroller.Delegate = this;
            
            if (_RefreshButton)
                _RefreshButton.onClick.AddListener(OnRefreshClicked);
            
            _serverManager.ServerListUpdated.AddListener(RefreshScroller);
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            
            UniTask.Void(async cancellationToken =>
            {
                await UniTask.Yield(cancellationToken);
                RefreshScroller();
            }, _cancellationTokenSource.Token);
            
            _serverManager.StartSearch();
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            if (_serverManager)
            {
                _serverManager.StopSearch();
                _serverManager.ServerListUpdated.RemoveListener(RefreshScroller);
            }
            
            if (_RefreshButton)
                _RefreshButton.onClick.RemoveListener(OnRefreshClicked);
        }

        private void OnRefreshClicked()
        {
            _serverManager.StartSearch();
            RefreshScroller();
        }

        private void RefreshScroller()
        {
            _currentServers.Clear();
            _currentServers.AddRange(_serverManager.ServerList);
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }

        public int GetNumberOfCells(EnhancedScroller scroller) => _currentServers.Count;

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex) => ((RectTransform)_ElementDisplayPrefab.transform).rect.height;

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cell = (ServerListElementDisplay) _Scroller.GetCellView(_ElementDisplayPrefab);
            if (!cell)
                return null;

            cell.SetRoom(_currentServers[dataIndex]);
            return cell;
        }
    }
}
