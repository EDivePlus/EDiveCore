// Author: Michal Petr
// Created: 20.10.2026

using System;
using EDIVE.Conditions;
using EDIVE.Core;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.LaserPointer
{
    [Serializable]
    public class LaserPointerActiveCondition : ABoolCondition
    {
        private LaserPointerController _controller;
        private IDisposable _registration;

        protected override bool GetValue() => AppCore.Services.TryGet<LaserPointerController>(out var controller) && controller.IsActive;

        public override void InitializeObserving()
        {
            _registration = AppCore.Services.WhenRegistered<LaserPointerController>(OnControllerRegistered);
        }

        public override void TerminateObserving()
        {
            _registration?.Dispose();
            _registration = null;

            if (_controller != null)
                _controller.ActiveHandChanged -= OnActiveHandChanged;
            _controller = null;
        }

        private void OnControllerRegistered(LaserPointerController controller)
        {
            _controller = controller;
            _controller.ActiveHandChanged += OnActiveHandChanged;
            InvokeStateChanged();
        }

        private void OnActiveHandChanged(InteractorHandedness hand) => InvokeStateChanged();
    }
}
