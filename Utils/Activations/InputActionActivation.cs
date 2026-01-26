// Author: František Holubec
// Created: 26.01.2026

#if INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EDIVE.Utils.Activations
{
    [Serializable]
    public class InputActionActivation : AWrapperActivation
    {
        [SerializeField]
        private InputActionReference _InputAction;
        
        protected override void StartListening()
        {
            if (_InputAction != null)
                _InputAction.action.performed += OnInputActionPerformed;
        }

        protected override void StopListening()
        {
            if (_InputAction != null)
                _InputAction.action.performed -= OnInputActionPerformed;
        }

        private void OnInputActionPerformed(InputAction.CallbackContext ctx) => InvokeListeners();
    }
}
#endif
