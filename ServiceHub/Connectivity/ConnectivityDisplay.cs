// Author: František Holubec
// Created: 02.10.2026

using System;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

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

        private ConnectivityService _service;
        private IDisposable _registration;

        private void OnEnable()
        {
            Refresh(ConnectivityState.Unknown, false);
            _registration = AppCore.Services.WhenRegistered<ServiceHubManager>(OnServiceHubRegistered);
        }

        private void OnDisable()
        {
            _registration?.Dispose();
            _registration = null;

            if (_service != null)
            {
                _service.StateChanged -= OnStateChanged;
                _service.IsCheckingChanged -= OnCheckingChanged;
            }
            _service = null;
        }

        private void OnServiceHubRegistered(ServiceHubManager serviceHub)
        {
            _service = serviceHub.Connectivity;
            if (_service == null)
                return;

            _service.StateChanged += OnStateChanged;
            _service.IsCheckingChanged += OnCheckingChanged;
            Refresh(_service.State, _service.IsChecking);
        }

        private void OnStateChanged(ConnectivityState _) => Refresh(_service.State, _service.IsChecking);
        private void OnCheckingChanged(bool _) => Refresh(_service.State, _service.IsChecking);

        private void Refresh(ConnectivityState state, bool isChecking)
        {
            if (_StateMultiState && _StateMultiState.State != state.ToString())
                _StateMultiState.SetState(state);
            if (_CheckingToggle) _CheckingToggle.SetState(state == ConnectivityState.Unknown || isChecking && state != ConnectivityState.Connected);
        }
    }
}
