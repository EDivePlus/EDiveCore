// Author: František Holubec
// Created: 2026-05-07

using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EDIVE.Input.Touch
{
    public class TouchControlsVisibility : MonoBehaviour
    {
        [Required] 
        [SerializeField]
        private AToggleState _ToggleState;
        
#pragma warning disable CS0414
        [SerializeField] 
        private bool _ShowInEditor = true;
#pragma warning restore CS0414
        
        [SerializeField]
        private OverrideMode _Override = OverrideMode.Auto;

        // Hidden while true, e.g. the menu is expanded
        [SerializeField]
        private AScriptableVariable<bool> _HideWhile;

        public enum OverrideMode
        {
            Auto, 
            ForceOn, 
            ForceOff
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            if (_HideWhile != null)
                _HideWhile.ValueChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            if (_HideWhile != null)
                _HideWhile.ValueChanged -= Refresh;
        }
        
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Touchscreen) 
                Refresh();
        }
        
        public void SetOverride(OverrideMode mode)
        {
            _Override = mode;
            Refresh();
        }

        [Button("Refresh")]
        public void Refresh()
        {
            if (_ToggleState == null) 
                return;
            var show = ShouldShow();
            _ToggleState.SetState(show);
        }

        private bool ShouldShow()
        {
            if (_HideWhile != null && _HideWhile.Value)
                return false;

            switch (_Override)
            {
                case OverrideMode.ForceOn:  return true;
                case OverrideMode.ForceOff: return false;
            }

            var hasTouch = Touchscreen.current != null;
#if UNITY_EDITOR
            return _ShowInEditor && hasTouch;
#else
            return Application.isMobilePlatform && hasTouch;
#endif
        }
    }
}
