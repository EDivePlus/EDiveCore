// Author: Michal Petr
// Created: 20.10.2026

using System;
using EDIVE.XRTools.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.LaserPointer
{
    public class XRLaserPointerInput : MonoBehaviour
    {
        [SerializeField]
        private LaserPointerController _Controller;

        [SerializeField]
        private Hand _LeftHand;

        [SerializeField]
        private Hand _RightHand;

        private void OnEnable()
        {
            _LeftHand.FireChanged += OnFireChanged;
            _RightHand.FireChanged += OnFireChanged;
            _LeftHand.Bind();
            _RightHand.Bind();
            _Controller.ActiveHandChanged += OnActiveHandChanged;
            OnActiveHandChanged(_Controller.ActiveHand);
        }

        private void OnDisable()
        {
            _Controller.ActiveHandChanged -= OnActiveHandChanged;
            _LeftHand.Unbind();
            _RightHand.Unbind();
            _LeftHand.FireChanged -= OnFireChanged;
            _RightHand.FireChanged -= OnFireChanged;
            _LeftHand.SetFarInteractionBlocked(this, false);
            _RightHand.SetFarInteractionBlocked(this, false);
        }

        private void OnActiveHandChanged(InteractorHandedness hand)
        {
            _LeftHand.SetFarInteractionBlocked(this, hand == InteractorHandedness.Left);
            _RightHand.SetFarInteractionBlocked(this, hand == InteractorHandedness.Right);
        }

        private void OnFireChanged(Hand hand, bool pressed)
        {
            var handedness = hand == _LeftHand ? InteractorHandedness.Left : InteractorHandedness.Right;
            if (_Controller.ActiveHand == handedness)
                _Controller.SetFiring(pressed);
        }

        [Serializable]
        private class Hand
        {
            [SerializeField]
            private InputActionReference _Fire;

            [SerializeField]
            private ControllerInputActionManager _InputActionManager;

            public event Action<Hand, bool> FireChanged;

            public void Bind()
            {
                if (_Fire == null)
                    return;
                _Fire.action.performed += OnFirePerformed;
                _Fire.action.canceled += OnFireCanceled;
            }

            public void Unbind()
            {
                if (_Fire == null)
                    return;
                _Fire.action.performed -= OnFirePerformed;
                _Fire.action.canceled -= OnFireCanceled;
            }

            public void SetFarInteractionBlocked(object requester, bool blocked)
            {
                if (_InputActionManager == null)
                    return;

                if (blocked)
                    _InputActionManager.BlockFarInteraction(requester);
                else
                    _InputActionManager.UnblockFarInteraction(requester);
            }

            private void OnFirePerformed(InputAction.CallbackContext _) => FireChanged?.Invoke(this, true);
            private void OnFireCanceled(InputAction.CallbackContext _) => FireChanged?.Invoke(this, false);
        }
    }
}
