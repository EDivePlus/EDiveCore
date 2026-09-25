// Author: Michal Petr
// Created: 24.09.2026

using System;
using EDIVE.Conditions;
using EDIVE.Core;

namespace EDIVE.Audio
{
    [Serializable]
    public class MicAllowedCondition : ABoolCondition
    {
        private AudioManager _manager;
        private IDisposable _registration;

        protected override bool GetValue() => AppCore.Services.TryGet<AudioManager>(out var manager) && manager.AllowMic;
        
        public override void InitializeObserving()
        {
            _registration = AppCore.Services.WhenRegistered<AudioManager>(OnManagerRegistered);
        }

        public override void TerminateObserving()
        {
            _registration?.Dispose();
            _registration = null;

            if (_manager != null)
                _manager.AllowMicChanged -= OnAllowMicChanged;
            _manager = null;
        }

        private void OnManagerRegistered(AudioManager manager)
        {
            _manager = manager;
            _manager.AllowMicChanged += OnAllowMicChanged;
            InvokeStateChanged();
        }
        
        private void OnAllowMicChanged(bool value) => InvokeStateChanged();
    }
}
