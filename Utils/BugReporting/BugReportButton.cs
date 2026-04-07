// Author: František Holubec
// Created: 25.11.2025

using DG.Tweening;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Utils.BugReporting
{
    public class BugReportButton : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;
        
        [SerializeField]
        private AToggleState _CanSendToggle;
        
        [SerializeField] 
        private Image _CooldownFill;
        
        private Tweener _tween;
        
        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(OnActivated);
            if (!AppCore.Services.TryGet<BugReportingManager>(out var bugReportingManager))
            {
                Debug.LogError("BugReportingManager not found", this);
                return;
            }
            
            bugReportingManager.ReportSent += RefreshState;
            RefreshState();
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(OnActivated);
            if (AppCore.Services.TryGet<BugReportingManager>(out var bugReportingManager))
            {
                bugReportingManager.ReportSent -= RefreshState;
            }
        }

        public void OnActivated()
        {
            if (!AppCore.Services.TryGet<BugReportingManager>(out var bugReportingManager))
                return;
            
            bugReportingManager.TrySendReport();
            RefreshState();
        }

        private void RefreshState()
        {
            _tween?.Kill();
            
            var bugReportingManager = AppCore.Services.Get<BugReportingManager>();
            if (bugReportingManager.CanSendReport)
            {
                if (_CanSendToggle != null)
                    _CanSendToggle.SetState(true);
                
                if (_CooldownFill != null)
                    _CooldownFill.fillAmount = 0;
            }else
            {
                if (_CanSendToggle != null)
                    _CanSendToggle.SetState(false);
                
                if (_CooldownFill != null)
                {
                    _CooldownFill.fillAmount = 1;
                    _tween = _CooldownFill
                        .DOFillAmount(0f, bugReportingManager.TimeUntilNextSend)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (_CanSendToggle != null)
                                _CanSendToggle.SetState(true);
                        });
                }
            }
        }
    }
}
