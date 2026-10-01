// Author: Michal Petr
// Created: 17.09.2026

using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EDIVE.NativeUtils;
using EDIVE.Tweening;
using EDIVE.UIElements.Layout;
using EDIVE.XRTools.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

        [SerializeField]
        private TweenAnimationController _ShowAnimation;

        [SerializeField]
        private TweenAnimationController _HideAnimation;

        private Hand _activeHand;
        private AActionWheelWedge[] _wedges;
        private AActionWheelWedge _hoveredWedge;
        private Canvas _canvas;
        private RadialLayout _layout;

        private Vector2 ThumbstickPosition => _activeHand?.ThumbstickPosition ?? Vector2.zero;
        private IEnumerable<AActionWheelWedge> VisibleWedges => _wedges.Where(wedge => wedge.gameObject.activeSelf);

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.enabled = false;
            _wedges = GetComponentsInChildren<AActionWheelWedge>(true);
            _layout = GetComponentInChildren<RadialLayout>(true);
        }

        private void OnDisable()
        {
            Hide(true);
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
                if (selectedWedge != null && selectedWedge.CanExecute)
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
            RefreshWedgeVisibility();
            _canvas.enabled = true;
            VisibleWedges.ForEach(wedge => wedge.OnShow());

            if (_HideAnimation != null)
                _HideAnimation.Kill();
            if (_ShowAnimation != null)
                _ShowAnimation.Play();

            hand.RequestThumbstickControl(this);
        }

        private void Hide(bool immediate = false)
        {
            SetHoveredWedge(null);
            _activeHand?.ReleaseThumbstickControl(this);
            VisibleWedges.ForEach(wedge => wedge.OnHide());
            _activeHand = null;

            if (_ShowAnimation != null)
                _ShowAnimation.Kill();

            if (immediate || _HideAnimation == null)
            {
                if (_HideAnimation != null)
                    _HideAnimation.Kill();
                _canvas.enabled = false;
                return;
            }

            _HideAnimation.Play().OnComplete(() => _canvas.enabled = false);
        }

        private void RefreshWedgeVisibility()
        {
            foreach (var wedge in _wedges)
                wedge.gameObject.SetActive(wedge.IsVisible);

            if (_layout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) _layout.transform);
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
