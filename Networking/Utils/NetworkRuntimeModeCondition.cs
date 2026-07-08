// Author: František Holubec
// Created: 08.07.2026

using System;
using System.Collections.Generic;
using EDIVE.Conditions;
using EDIVE.Core;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    [Serializable]
    public class NetworkRuntimeModeCondition : ABoolCondition
    {
        [SerializeField]
        private List<NetworkRuntimeMode> _RuntimeModes = new() {NetworkRuntimeMode.Host};

        private MasterNetworkManager _manager;

        protected override bool GetValue()
        {
            return AppCore.Services.TryGet<MasterNetworkManager>(out var manager) && _RuntimeModes.Contains(manager.RuntimeMode);
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
                _manager.RuntimeModeChanged -= OnRuntimeModeChanged;

            _manager = manager;

            if (_manager != null)
                _manager.RuntimeModeChanged += OnRuntimeModeChanged;
        }

        private void OnRuntimeModeChanged(NetworkRuntimeMode mode) => InvokeStateChanged();
    }
}
