// Author: František Holubec
// Created: 19.10.2026

using System;
using EDIVE.Conditions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows.Screens
{
    [Serializable]
    public class WindowOpenCondition : ABoolCondition
    {
        public enum OpenState
        {
            // Also minimized
            Open,
            // Open and not minimized
            Shown,
            // Top of its layer
            Front
        }

        [Required]
        [SerializeField]
        private ScreenRoot _Screen;

        [Required]
        [SerializeField]
        private WindowDefinition _Window;

        [SerializeField]
        private OpenState _RequiredState;

        protected override bool GetValue()
        {
            if (_Screen == null || _Window == null)
                return false;

            var handle = _Screen.FindHandle(_Window);
            if (handle == null)
                return false;

            return _RequiredState switch
            {
                OpenState.Shown => !handle.IsMinimized,
                OpenState.Front => _Screen.IsFront(handle),
                _ => true
            };
        }

        public override void InitializeObserving()
        {
            base.InitializeObserving();
            if (_Screen != null)
                _Screen.Changed += OnScreenChanged;
        }

        public override void TerminateObserving()
        {
            base.TerminateObserving();
            if (_Screen != null)
                _Screen.Changed -= OnScreenChanged;
        }

        private void OnScreenChanged(ScreenRoot screen) => InvokeStateChanged();
    }
}
