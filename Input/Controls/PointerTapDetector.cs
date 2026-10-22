// Author: Michal Petr
// Created: 22.10.2026

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

namespace EDIVE.Input.Controls
{
    // Short left clicks and touches of any finger that didn't start over UI, works next to held joysticks
    public class PointerTapDetector : MonoBehaviour
    {
        [Tooltip("Pixels the pointer may move")]
        [MinValue(0f)]
        [SerializeField]
        private float _MaxMovement = 20f;

        [MinValue(0f)]
        [SuffixLabel("s", true)]
        [SerializeField]
        private float _MaxDuration = 0.4f;

        private static readonly List<RaycastResult> RAYCAST_RESULTS = new();

        private Vector2 _mousePressPosition;
        private double _mousePressTime;

        public event Action<Vector2> Tapped;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            EnhancedTouch.Touch.onFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _mousePressPosition = mouse.position.ReadValue();
                _mousePressTime = Time.unscaledTimeAsDouble;
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                TryTap(_mousePressPosition, mouse.position.ReadValue(), Time.unscaledTimeAsDouble - _mousePressTime);
            }
        }

        private void OnFingerUp(Finger finger)
        {
            var touch = finger.lastTouch;
            TryTap(touch.startScreenPosition, touch.screenPosition, touch.time - touch.startTime);
        }

        private void TryTap(Vector2 start, Vector2 end, double duration)
        {
            if (duration > _MaxDuration || Vector2.Distance(start, end) > _MaxMovement || IsOverUI(start))
                return;
            Tapped?.Invoke(start);
        }

        private static bool IsOverUI(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            RAYCAST_RESULTS.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, RAYCAST_RESULTS);
            return RAYCAST_RESULTS.Count > 0;
        }
    }
}
