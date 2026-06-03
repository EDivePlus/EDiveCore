// Author: František Holubec
// Created: 03.06.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.SystemInformation
{
    public class SystemInfoDisplay : MonoBehaviour
    {
        [SerializeField]
        private SystemInfoManager _Manager;

        [SerializeField]
        private SystemInfoCategoryDisplay _DisplayPrefab;

        [SerializeField]
        private Transform _DisplayContainer;

        [SerializeField]
        private List<SystemInfoCategoryDisplay> _Displays = new();

        [SerializeField]
        private bool _RefreshPeriodically;
        
        [SerializeField]
        private float _RefreshInterval = 1f;
        
        [SerializeField]
        private float _IndentEm = 20f;
        
        private CancellationTokenSource _refreshCts;
        
        private void OnEnable()
        {
            RefreshList();
            
            if (_RefreshPeriodically)
            {
                _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                RefreshTask(_refreshCts.Token).Forget();
            }
        }

        private void OnDisable()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = null;
        }

        private async UniTaskVoid RefreshTask(CancellationToken cts)
        {
            while (enabled)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_RefreshInterval), cancellationToken: cts);
                RefreshList();
            }
        }
        
        [Button]
        private void RefreshList()
        {
            if (_Manager == null)
                return;
            
            var categories = _Manager.Categories;
            if (categories == null || categories.Count == 0)
                return;
            
            for (var i = 0; i < categories.Count; i++)
            {
                if (i >= _Displays.Count)
                {
                    var display = Instantiate(_DisplayPrefab, _DisplayContainer);
                    _Displays.Add(display);
                }

                _Displays[i].SetCategory(categories[i], _IndentEm);
                _Displays[i].gameObject.SetActive(true);
            }

            for (var i = categories.Count; i < _Displays.Count; i++)
            {
                _Displays[i].gameObject.SetActive(false);
            }
        }
    }
}
