// Author: František Holubec
// Created: 29.10.2026

using System;
using EDIVE.Core;
using PurrNet.Transports;

namespace EDIVE.Networking.ServiceHub
{
    // Connected to a session
    [Serializable]
    public class NetworkRequirement : ARequirement
    {
        private IDisposable _registration;
        private MasterNetworkManager _network;

        public override bool IsMet => _network && _network.ConnectionState == ConnectionState.Connected;

        protected override void OnBind() => _registration = AppCore.Services.WhenRegistered<MasterNetworkManager>(SetNetwork);

        protected override void OnUnbind()
        {
            _registration?.Dispose();
            _registration = null;
            SetNetwork(null);
        }

        private void SetNetwork(MasterNetworkManager network)
        {
            if (_network)
                _network.ConnectionStateChanged -= OnConnectionStateChanged;
            _network = network;
            if (_network)
                _network.ConnectionStateChanged += OnConnectionStateChanged;
            NotifyChanged();
        }

        private void OnConnectionStateChanged(ConnectionState state) => NotifyChanged();
    }
}
