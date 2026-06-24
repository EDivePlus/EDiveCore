// Author: František Holubec
// Created: 03.06.2026

using System;
using System.Threading;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Utils.SystemInformation
{
    public class SystemInfoDisplay : MonoBehaviour
    {
        [SerializeField]
        private SystemInfoManager _Manager;

        [SerializeField]
        private TMP_Text _Text;
        
        [SerializeField]
        private bool _RefreshPeriodically;
        
        [SerializeField]
        private float _RefreshInterval = 1f;
        
        [SerializeField]
        private RichTextUnitField _LabelIndent = new(0.5f, RichTextUnit.Em);
        
        [SerializeField]
        private RichTextUnitField _ValueIndent = new(20, RichTextUnit.Em);
        
        private CancellationTokenSource _refreshCts;
        
        private void OnEnable()
        {
            RefreshDisplay();
            
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
                RefreshDisplay();
            }
        }
        
        [Button]
        private void RefreshDisplay()
        {
            if (_Manager == null)
                return;
            
            var categories = _Manager.Categories;
            if (categories == null || categories.Count == 0)
                return;

            using var sb = ZString.CreateStringBuilder(true);
            foreach (var category in categories)
            {
                sb.AppendLine(category.Name.Bold());
                foreach (var entry in category.Entries)
                {
                    sb.Append(entry.Name.Indent(_LabelIndent));
                    sb.Append(entry.GetValue().Indent(_ValueIndent));
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            var finalText = sb.ToString();
                
            if (_Text) 
                _Text.text = finalText;
        }
    }
}
