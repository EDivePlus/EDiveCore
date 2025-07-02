// Author: František Holubec
// Created: 13.06.2025

using System.Collections.Generic;
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

        private readonly List<Room> _currentRooms = new();
        private LightReflectiveMirrorTransport _lrm;

        private void OnEnable()
        {
            AppCore.Services.WhenRegistered<MasterNetworkManager>(Initialize);
        }

        private void Initialize(MasterNetworkManager manager)
        {
            _Scroller.Delegate = this;
            if (!manager.TryGetTransport<LightReflectiveMirrorTransport>(out var lrm))
                return;

            if (_RefreshButton)
                _RefreshButton.onClick.AddListener(OnRefreshClicked);

            _lrm = lrm;
            _lrm.serverListUpdated.AddListener(RefreshScroller);

            UniTask.Void(async cancellationToken =>
            {
                await UniTask.Yield(cancellationToken);
                RefreshScroller();
            }, destroyCancellationToken);
        }

        private void OnDisable()
        {
            if (_RefreshButton)
                _RefreshButton.onClick.RemoveListener(OnRefreshClicked);

            if (_lrm)
                _lrm.serverListUpdated.RemoveListener(RefreshScroller);
        }

        private void OnRefreshClicked()
        {
            _lrm.RequestServerList();
            RefreshScroller();
        }

        private void RefreshScroller()
        {
            _currentRooms.Clear();
            _currentRooms.AddRange(_lrm.relayServerList);
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }

        public int GetNumberOfCells(EnhancedScroller scroller) => _currentRooms.Count;

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex) => ((RectTransform)_ElementDisplayPrefab.transform).rect.height;

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cell = (ServerListElementDisplay) _Scroller.GetCellView(_ElementDisplayPrefab);
            if (!cell)
                return null;

            cell.SetRoom(_currentRooms[dataIndex]);
            return cell;
        }
    }
}
