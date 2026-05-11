// Author: František Holubec
// Created: 2026-05-07

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace EDIVE.Input.Touch
{
    public class LookPad : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        protected override string controlPathInternal { get => _ControlPath; set => _ControlPath = value; }

        [InputControl(layout = "Vector2")] 
        [SerializeField]
        private string _ControlPath;
   
        [Tooltip("Multiplier on touch delta. ~0.005 = a 200px swipe maps to a full stick deflection.")]
        [MinValue(0f)]
        [SerializeField]
        private float _Sensitivity = 0.005f;
        
        [Tooltip("Total pixels of motion below which a press+release is treated as a tap.")]
        [MinValue(0f)]
        [SerializeField]
        private float _TapThreshold = 10f;

        [Tooltip("Invert horizontal drag direction.")]
        [SerializeField]
        private bool _InvertX;

        [Tooltip("Invert vertical drag direction. Common for \"drag the world\" feel — swipe up tilts the camera down.")]
        [SerializeField]
        private bool _InvertY;
        
        public event Action<Vector2> Tapped;

        private int _activePointerId = -1;
        private Vector2 _pressPosition;
        private Vector2 _accumDelta;
        private float _totalMovement;
        private bool _hasFreshDelta;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != -1) return;
            _activePointerId = eventData.pointerId;
            _pressPosition = eventData.position;
            _accumDelta = Vector2.zero;
            _totalMovement = 0f;
            _hasFreshDelta = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            var delta = eventData.delta;
            _totalMovement += delta.magnitude;
            if (_InvertX) delta.x = -delta.x;
            if (_InvertY) delta.y = -delta.y;
            _accumDelta += delta;
            _hasFreshDelta = true;

            SendValueToControl(_accumDelta * _Sensitivity);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            _activePointerId = -1;

            SendValueToControl(Vector2.zero);
            _accumDelta = Vector2.zero;
            _hasFreshDelta = false;

            if (_totalMovement < _TapThreshold)
            {
                Tapped?.Invoke(_pressPosition);
            }
        }

        private void LateUpdate()
        {
            if (_hasFreshDelta)
            {
                // Drag fired this frame — let the delta event propagate to next frame's read.
                // Don't queue zero in the same frame; the InputSystem batches state events and
                // processes them at the start of the next frame in queue order. A delta+zero
                // pair within one frame would resolve to zero and the camera would never see the delta.
                _accumDelta = Vector2.zero;
                _hasFreshDelta = false;
                return;
            }

            if (_activePointerId != -1)
            {
                // Finger pressed but not moving this frame — neutralize the stick so the camera
                // doesn't keep rotating from the previous frame's delta.
                SendValueToControl(Vector2.zero);
            }
            // Else: finger up, OnPointerUp already sent zero — nothing to do.
        }
    }
}
