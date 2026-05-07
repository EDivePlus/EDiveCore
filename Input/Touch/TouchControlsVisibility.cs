// Author: František Holubec
// Created: 2026-05-07

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
        
        [SerializeField] 
        private bool _ShowInEditor = true;
        
        [SerializeField] 
        private OverrideMode _Override = OverrideMode.Auto;

        public enum OverrideMode
        {
            Auto, 
            ForceOn, 
            ForceOff
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            Refresh();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
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
