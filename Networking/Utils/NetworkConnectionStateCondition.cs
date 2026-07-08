// Author: František Holubec
// Created: 08.07.2026

using System;
using System.Collections.Generic;
using EDIVE.Conditions;
using EDIVE.Core;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    [Serializable]
    public class NetworkConnectionStateCondition : ABoolCondition
    {
        [SerializeField]
        private List<ConnectionState> _ConnectionStates = new() {ConnectionState.Connected};

        private MasterNetworkManager _manager;

        protected override bool GetValue()
        {
            return AppCore.Services.TryGet<MasterNetworkManager>(out var manager) && _ConnectionStates.Contains(manager.ConnectionState);
        }

        public override void InitializeObserving()
        {
            base.InitializeObserving();
            AppCore.Services.SubscribeOnChangeWithInitial<MasterNetworkManager>(OnManagerChanged);
        }

        public override void TerminateObserving()
        {
            base.TerminateObserving();
            AppCore.Services.UnsubscribeOnChange<MasterNetworkManager>(OnManagerChanged);
            SetManager(null);
        }

        private void OnManagerChanged(MasterNetworkManager manager)
        {
            SetManager(manager);
            InvokeStateChanged();
        }

        private void SetManager(MasterNetworkManager manager)
        {
            if (_manager == manager)
                return;

            if (_manager != null)
                _manager.ConnectionStateChanged -= OnConnectionStateChanged;

            _manager = manager;

            if (_manager != null)
                _manager.ConnectionStateChanged += OnConnectionStateChanged;
        }

        private void OnConnectionStateChanged(ConnectionState state) => InvokeStateChanged();
    }
}
