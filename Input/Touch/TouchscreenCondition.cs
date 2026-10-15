// Author: František Holubec
// Created: 15.10.2026

using System;
using EDIVE.Conditions;
using UnityEngine.InputSystem;

namespace EDIVE.Input
{
    // True while a touch screen is connected, e.g. phones, tablets and touch laptops
    [Serializable]
    public class TouchscreenCondition : ABoolCondition
    {
        protected override bool GetValue() => Touchscreen.current != null;

        public override void InitializeObserving() => InputSystem.onDeviceChange += OnDeviceChange;
        public override void TerminateObserving() => InputSystem.onDeviceChange -= OnDeviceChange;

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Touchscreen)
                InvokeStateChanged();
        }
    }
}
