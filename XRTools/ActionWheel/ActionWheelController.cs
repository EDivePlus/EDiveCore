// Author: Michal Petr
// Created: 17.09.2026

using System;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.XRTools.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EDIVE.XRTools.ActionWheel
{
    [RequireComponent(typeof(Canvas))]
    public class ActionWheelController : MonoBehaviour
    {
        [SerializeField]
        private Hand _LeftHand;

        [SerializeField]
        private Hand _RightHand;

        [SerializeField]
        private Vector3 _PositionOffset = new(0f, 0.06f, 0.04f);

        [SerializeField]
        private Vector3 _RotationOffset = new(60f, 0f, 0f);

        [SerializeField]
        [Range(0f, 1f)]
        private float _HoverThreshold = 0.5f;
        
        private Hand _activeHand;
        private AActionWheelWedge[] _wedges;
        private AActionWheelWedge _hoveredWedge;
        private Canvas _canvas;

        private Vector2 ThumbstickPosition => _activeHand?.ThumbstickPosition ?? Vector2.zero;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.enabled = false;
            _wedges = GetComponentsInChildren<AActionWheelWedge>();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Update()
        {
            if (_activeHand != null)
            {
                if (_activeHand.IsPressed)
                {
                    UpdateHover();
                    return;
                }

                var selectedWedge = _hoveredWedge;
                Hide();
                if (selectedWedge != null)
                    selectedWedge.ExecuteActions();
                return;
            }

            if (_LeftHand.IsPressed)
                Show(_LeftHand);
            else if (_RightHand.IsPressed)
                Show(_RightHand);
        }

        private void Show(Hand hand)
        {
            _activeHand = hand;
            transform.SetParent(hand.Anchor, false);
            transform.SetLocalPositionAndRotation(_PositionOffset, Quaternion.Euler(_RotationOffset));
            _canvas.enabled = true;
            _wedges.ForEach(wedge => wedge.OnShow());
            hand.RequestThumbstickControl(this);
        }

        private void Hide()
        {
            SetHoveredWedge(null);
            _activeHand?.ReleaseThumbstickControl(this);
            _wedges.ForEach(wedge => wedge.OnHide());
            _activeHand = null;
            _canvas.enabled = false;
        }

        private void UpdateHover()
        {
            var thumbstick = ThumbstickPosition;
            SetHoveredWedge(thumbstick.magnitude >= _HoverThreshold ? FindWedge(thumbstick) : null);
        }

        private AActionWheelWedge FindWedge(Vector2 direction)
        {
            var angle = Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
            return _wedges.FirstOrDefault(wedge => wedge != null && wedge.isActiveAndEnabled && wedge.ContainsAngle(angle));
        }

        private void SetHoveredWedge(AActionWheelWedge wedge)
        {
            if (_hoveredWedge == wedge)
                return;

            if (_hoveredWedge != null)
                _hoveredWedge.SetHovered(false);

            _hoveredWedge = wedge;

            if (_hoveredWedge != null)
                _hoveredWedge.SetHovered(true);
        }

        [Serializable]
        private class Hand
        {
            [SerializeField]
            private InputActionReference _ThumbstickClick;

            [SerializeField]
            private InputActionReference _Thumbstick;

            [SerializeField]
            private Transform _Anchor;

            [SerializeField]
            private ControllerInputActionManager _InputActionManager;

            public Transform Anchor => _Anchor;
            public bool IsPressed => _Anchor != null && _ThumbstickClick != null && _ThumbstickClick.action.IsPressed();
            public Vector2 ThumbstickPosition => _Thumbstick != null ? _Thumbstick.action.ReadValue<Vector2>() : Vector2.zero;

            public void RequestThumbstickControl(object requester)
            {
                if (_InputActionManager != null)
                    _InputActionManager.RequestThumbstickControl(requester);
            }

            public void ReleaseThumbstickControl(object requester)
            {
                if (_InputActionManager != null)
                    _InputActionManager.ReleaseThumbstickControl(requester);
            }
        }
    }
}
