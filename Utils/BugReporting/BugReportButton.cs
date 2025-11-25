// Author: František Holubec
// Created: 25.11.2025

using DG.Tweening;
using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Utils.BugReporting
{
    public class BugReportButton : MonoBehaviour
    {
        [SerializeField]
        private Button _Button;
        
        [SerializeField] 
        private Image _CooldownFill;
        
        private Tweener _tween;
        
        private void OnEnable()
        {
            if (_Button != null)
                _Button.onClick.AddListener(OnButtonClick);

            if (AppCore.Services.TryGet<BugReportingManager>(out var bugReportingManager))
            {
                bugReportingManager.ReportSent += RefreshState;
                RefreshState();
            }
        }

        private void OnDisable()
        {
            if (_Button != null)
                _Button.onClick.RemoveListener(OnButtonClick);
            
            if (AppCore.Services.TryGet<BugReportingManager>(out var bugReportingManager))
            {
                bugReportingManager.ReportSent -= RefreshState;
            }
        }

        public void OnButtonClick()
        {
            AppCore.Services.Get<BugReportingManager>().TrySendReport();
            RefreshState();
        }

        private void RefreshState()
        {
            _tween?.Kill();
            
            var bugReportingManager = AppCore.Services.Get<BugReportingManager>();
            if (bugReportingManager.CanSendReport)
            {
                if (_Button != null)
                    _Button.interactable = true;
                
                if (_CooldownFill != null)
                    _CooldownFill.fillAmount = 0;
            }else
            {
                if (_Button != null)
                    _Button.interactable = false;

                if (_CooldownFill != null)
                {
                    _CooldownFill.fillAmount = 1;
                    _tween = _CooldownFill
                        .DOFillAmount(0f, bugReportingManager.TimeUntilNextSend)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (_Button != null)
                                _Button.interactable = true;
                        });
                }
            }
        }
    }
}
