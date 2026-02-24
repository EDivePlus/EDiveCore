// Author: Radim Holub
// Created: 04.12.2025

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Utils.Activations;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.UI
{
    public class ReplayListDisplay : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [SerializeField]
        public EnhancedScroller _Scroller;
        
        [SerializeField] 
        private ReplayListElementDisplay _ElementPrefab;
        
        [SerializeReference] 
        private IActivation _RefreshActivation;
        
        private readonly List<ReplayRecordInfo> _currentRecords = new();
        
        private void OnEnable()
        {
            _Scroller.Delegate = this;
            _RefreshActivation?.RegisterActivationListener(RefreshList);
        }

        private void Start()
        {
            RefreshList();
        }

        private void OnDisable()
        {
            _RefreshActivation?.UnregisterActivationListener(RefreshList);
        }

        [Button]
        private void RefreshList()
        {
            if (!AppCore.Services.TryGet<ReplayController>(out var controller))
                return;
            
            UniTask.Void(async () =>
            {
                var records = await controller.GetSavedRecords();
                _currentRecords.Clear();
                _currentRecords.AddRange(records);
                _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
            });
        }
        
        private void OnReplayRecordedSelected(ReplayRecordInfo info)
        {
            if (AppCore.Services.TryGet<ReplayController>(out var controller))
                controller.LoadRecord(info);
        }

        public int GetNumberOfCells(EnhancedScroller scroller) => _currentRecords.Count;

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex) => ((RectTransform)_ElementPrefab.transform).rect.height;

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cell = (ReplayListElementDisplay) _Scroller.GetCellView(_ElementPrefab);
            if (!cell)
                return null;

            cell.SetReplay(_currentRecords[dataIndex], OnReplayRecordedSelected);
            return cell;
        }
    }
}
