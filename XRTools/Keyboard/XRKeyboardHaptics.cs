﻿// Author: František Holubec
// Created: 18.10.2026

using EDIVE.Input.Keyboard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace EDIVE.XRTools.Keyboard
{
    // Short pulse on the controller that pressed a key
    public class XRKeyboardHaptics : MonoBehaviour
    {
        [SerializeField]
        private VirtualKeyboardController _Keyboard;

        [SerializeField]
        [Range(0, 1)]
        private float _Amplitude = 0.25f;

        [SerializeField]
        private float _Duration = 0.02f;

        private void Awake()
        {
            if (_Keyboard == null)
                _Keyboard = GetComponentInParent<VirtualKeyboardController>(true);
        }

        private void OnEnable()
        {
            if (_Keyboard != null)
                _Keyboard.KeyDown += OnKeyDown;
        }

        private void OnDisable()
        {
            if (_Keyboard != null)
                _Keyboard.KeyDown -= OnKeyDown;
        }

        private void OnKeyDown(VirtualKeyboardKey key, PointerEventData eventData)
        {
            if (eventData is not TrackedDeviceEventData { interactor: Component interactor })
                return;

            var player = interactor.GetComponentInParent<HapticImpulsePlayer>();
            if (player != null)
                player.SendHapticImpulse(_Amplitude, _Duration);
            else if (interactor is XRBaseInputInteractor inputInteractor)
                inputInteractor.SendHapticImpulse(_Amplitude, _Duration);
        }
    }
}
