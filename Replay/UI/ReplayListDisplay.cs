// Author: Radim Holub
// Created: 04.12.2025

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.UIElements.RecyclableScroller;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.UI
{
    public class ReplayListDisplay : MonoBehaviour
    {
        [SerializeField]
        public RecyclableScroller _Scroller;
        
        [SerializeField] 
        private ReplayListElementDisplay _ElementPrefab;
        
        [SerializeReference] 
        private IActivation _RefreshActivation;
        
        private readonly List<AReplayRecordMeta> _currentRecords = new();
        
        private void OnEnable()
        {
            _Scroller.Source = RecyclableScrollerSource.CreateForSinglePrefab(
                _Scroller,
                _ElementPrefab,
                _currentRecords,
                (item, record) => item.SetReplay(record, OnReplayRecordedSelected));
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
        
        private void OnReplayRecordedSelected(AReplayRecordMeta meta)
        {
            if (AppCore.Services.TryGet<ReplayController>(out var controller))
                controller.LoadRecord(meta);
        }

    }
}
