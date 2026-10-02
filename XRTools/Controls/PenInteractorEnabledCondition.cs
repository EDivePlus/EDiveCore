// Author: František Holubec
// Created: 02.10.2026

using System;
using EDIVE.Conditions;
using EDIVE.Core;

namespace EDIVE.XRTools.Controls
{
    [Serializable]
    public class PenInteractorEnabledCondition : ABoolCondition
    {
        private PenInteractorManager _manager;
        private IDisposable _registration;

        protected override bool GetValue() => AppCore.Services.TryGet<PenInteractorManager>(out var manager) && manager.PenInteractorEnabled;

        public override void InitializeObserving()
        {
            _registration = AppCore.Services.WhenRegistered<PenInteractorManager>(OnManagerRegistered);
        }

        public override void TerminateObserving()
        {
            _registration?.Dispose();
            _registration = null;

            if (_manager != null)
                _manager.PenInteractorEnabledChanged -= OnPenInteractorEnabledChanged;
            _manager = null;
        }

        private void OnManagerRegistered(PenInteractorManager manager)
        {
            _manager = manager;
            _manager.PenInteractorEnabledChanged += OnPenInteractorEnabledChanged;
            InvokeStateChanged();
        }

        private void OnPenInteractorEnabledChanged(bool value) => InvokeStateChanged();
    }
}
