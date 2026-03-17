// Author: František Holubec
// Created: 17.03.2026

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace EDIVE.XRTools.HandGestures
{
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRInputDeviceButtonReader)]
    public class HandGestureButtonReader : MonoBehaviour, IXRInputButtonReader
    {
        [SerializeField]
        private HandGestureDetector _Detector;

        private bool _isPerformed;
        private int _lastPerformedFrame;
        private int _lastCompletedFrame;

        private void OnEnable()
        {
            if (_Detector == null) return;

            _Detector.PerformStarted += OnGesturePerformed;
            _Detector.PerformEnded += OnGestureEnded;
            
            _isPerformed = _Detector.IsPerforming;
        }

        private void OnDisable()
        {
            if (_Detector == null) return;

            _Detector.PerformStarted -= OnGesturePerformed;
            _Detector.PerformEnded -= OnGestureEnded;
            
            _isPerformed = false;
        }

        private void OnGesturePerformed()
        {
            if (_isPerformed) return;
            
            _isPerformed = true;
            _lastPerformedFrame = Time.frameCount;
        }

        private void OnGestureEnded()
        {
            if (!_isPerformed) return;
            
            _isPerformed = false;
            _lastCompletedFrame = Time.frameCount;
        }
        
        public float ReadValue()
        {
            return _isPerformed ? 1f : 0f;
        }

        public bool TryReadValue(out float value)
        {
            value = ReadValue();
            return true;
        }

        public bool ReadIsPerformed()
        {
            return _isPerformed;
        }

        public bool ReadWasPerformedThisFrame()
        {
            return _lastPerformedFrame == Time.frameCount;
        }

        public bool ReadWasCompletedThisFrame()
        {
            return _lastCompletedFrame == Time.frameCount;
        }
    }
}
