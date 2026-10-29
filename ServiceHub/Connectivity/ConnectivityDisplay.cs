// Author: František Holubec
// Created: 02.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Connectivity
{
    public class ConnectivityDisplay : MonoBehaviour
    {
        [SerializeField]
        [Required]
        [PropertyTooltip("State ids match ConnectivityState names.")]
        private AMultiState _StateMultiState;

        [SerializeField]
        private AToggleState _CheckingToggle;

        [SerializeField]
        [PropertyTooltip("Optional, click checks now.")]
        private Button _CheckButton;

        [SerializeField]
        [SuffixLabel("s", true)]
        private float _ClickCooldown = 1f;

        [SerializeField]
        [SuffixLabel("s", true)]
        [PropertyTooltip("Shows checking at least this long after a click.")]
        private float _MinCheckingTime = 0.3f;

        private ConnectivityService _service;
        private IDisposable _registration;
        private float _nextClickTime;
        private float _manualCheckEnd;
        private bool _manualCheck;

        private void Awake()
        {
            if (_CheckButton) _CheckButton.onClick.AddListener(OnCheckClicked);
        }

        private void OnDestroy()
        {
            if (_CheckButton) _CheckButton.onClick.RemoveListener(OnCheckClicked);
        }

        private void OnEnable()
        {
            Refresh(ConnectivityState.Unknown, false);
            _registration = AppCore.Services.WhenRegistered<ServiceHubManager>(OnServiceHubRegistered);
        }

        private void OnDisable()
        {
            _registration?.Dispose();
            _registration = null;
            Unbind();
        }

        private void Unbind()
        {
            if (_service != null)
            {
                _service.StateChanged -= OnStateChanged;
                _service.IsCheckingChanged -= OnCheckingChanged;
            }

            _service = null;
        }

        private void OnServiceHubRegistered(ServiceHubManager serviceHub)
        {
            Unbind();
            if (!isActiveAndEnabled)
                return;

            _service = serviceHub.Connectivity;
            if (_service == null)
                return;

            _service.StateChanged += OnStateChanged;
            _service.IsCheckingChanged += OnCheckingChanged;
            Refresh(_service.State, _service.IsChecking);
        }

        // Can still fire after unbinding when the same event disabled this display
        private void OnStateChanged(ConnectivityState _)
        {
            if (_service != null) Refresh(_service.State, _service.IsChecking);
        }

        private void OnCheckingChanged(bool _)
        {
            if (_service != null) Refresh(_service.State, _service.IsChecking);
        }

        private void OnCheckClicked()
        {
            if (_service == null || Time.unscaledTime < _nextClickTime)
                return;

            _nextClickTime = Time.unscaledTime + _ClickCooldown;
            _manualCheckEnd = Time.unscaledTime + _MinCheckingTime;
            _service.RequestCheck();
            if (_manualCheck)
                return;

            _manualCheck = true;
            Refresh(_service.State, _service.IsChecking);
            EndManualCheckAsync().Forget();
        }

        private async UniTaskVoid EndManualCheckAsync()
        {
            var cancelled = await UniTask.WaitUntil(() => Time.unscaledTime >= _manualCheckEnd && (_service == null || !_service.IsChecking), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();
            if (cancelled)
                return;

            _manualCheck = false;
            if (_service != null)
                Refresh(_service.State, _service.IsChecking);
        }

        private void Refresh(ConnectivityState state, bool isChecking)
        {
            // Unknown is the checking icon
            var shownState = _manualCheck ? ConnectivityState.Unknown : state;
            if (_StateMultiState && _StateMultiState.State != shownState.ToString())
                _StateMultiState.SetState(shownState);
            if (_CheckingToggle) _CheckingToggle.SetState(_manualCheck || state == ConnectivityState.Unknown || isChecking && state != ConnectivityState.Connected);
        }
    }
}
