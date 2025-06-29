// Author: František Holubec
// Created: 13.06.2025

using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using LightReflectiveMirror;
using Mirror;
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

        private void Start()
        {
            _Scroller.Delegate = this;
            RefreshScroller();
        }

        private void OnEnable()
        {
            if (_RefreshButton)
                _RefreshButton.onClick.AddListener(OnRefreshClicked);
            if (Transport.active is LightReflectiveMirrorTransport lrm)
            {
                _lrm = lrm;
                _lrm.serverListUpdated.AddListener(RefreshScroller);
            }
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
            if(!_lrm)
                return;

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
