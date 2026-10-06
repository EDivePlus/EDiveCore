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
    public enum ActionWheelMode
    {
        Toggle,
        Hold
    }

    [RequireComponent(typeof(Canvas))]
    public class ActionWheelController : MonoBehaviour
    {
        [SerializeField]
        private ActionWheelMode _Mode = ActionWheelMode.Toggle;

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

        private IEnumerable<AActionWheelWedge> VisibleWedges => _wedges.Where(wedge => wedge.gameObject.activeSelf);

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.enabled = false;
            _wedges = GetComponentsInChildren<AActionWheelWedge>(true);
            _layout = GetComponentInChildren<RadialLayout>(true);
        }

        private void OnEnable()
        {
            Subscribe(_LeftHand);
            Subscribe(_RightHand);
        }

        private void OnDisable()
        {
            Unsubscribe(_LeftHand);
            Unsubscribe(_RightHand);
            Hide(true);
        }

        private void Subscribe(Hand hand)
        {
            hand.Pressed += OnPressed;
            hand.Released += OnReleased;
            hand.CancelPressed += OnCancelPressed;
            hand.ThumbstickChanged += OnThumbstickChanged;
            hand.Bind();
        }

        private void Unsubscribe(Hand hand)
        {
            hand.Unbind();
            hand.Pressed -= OnPressed;
            hand.Released -= OnReleased;
            hand.CancelPressed -= OnCancelPressed;
            hand.ThumbstickChanged -= OnThumbstickChanged;
        }

        private void OnPressed(Hand hand)
        {
            if (_activeHand == null)
                Show(hand);
            else if (_Mode == ActionWheelMode.Toggle && hand == _activeHand)
                Hide();
        }

        private void OnReleased(Hand hand)
        {
            if (_Mode == ActionWheelMode.Hold && hand == _activeHand)
                HideAndExecute();
        }

        private void OnCancelPressed(Hand hand)
        {
            if (hand == _activeHand)
                Hide();
        }

        private void OnThumbstickChanged(Hand hand, Vector2 value)
        {
            if (hand != _activeHand)
                return;

            var previousHovered = _hoveredWedge;
            SetHoveredWedge(value.magnitude >= _HoverThreshold ? FindWedge(value) : null);

            if (_Mode == ActionWheelMode.Toggle && previousHovered != null && _hoveredWedge == null)
            {
                SetHoveredWedge(previousHovered);
                HideAndExecute();
            }
        }

        private void HideAndExecute()
        {
            var selectedWedge = _hoveredWedge;
            Hide();
            if (selectedWedge != null && selectedWedge.CanExecute)
                selectedWedge.ExecuteActions();
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
            private InputActionReference _Cancel;

            [SerializeField]
            private Transform _Anchor;

            [SerializeField]
            private ControllerInputActionManager _InputActionManager;

            public Transform Anchor => _Anchor;

            public event Action<Hand> Pressed;
            public event Action<Hand> Released;
            public event Action<Hand> CancelPressed;
            public event Action<Hand, Vector2> ThumbstickChanged;

            public void Bind()
            {
                if (_Anchor == null)
                    return;

                if (_ThumbstickClick != null)
                {
                    _ThumbstickClick.action.performed += OnClickPerformed;
                    _ThumbstickClick.action.canceled += OnClickCanceled;
                }

                if (_Thumbstick != null)
                {
                    _Thumbstick.action.performed += OnThumbstick;
                    _Thumbstick.action.canceled += OnThumbstick;
                }

                if (_Cancel != null)
                    _Cancel.action.performed += OnCancelPerformed;
            }

            public void Unbind()
            {
                if (_ThumbstickClick != null)
                {
                    _ThumbstickClick.action.performed -= OnClickPerformed;
                    _ThumbstickClick.action.canceled -= OnClickCanceled;
                }

                if (_Thumbstick != null)
                {
                    _Thumbstick.action.performed -= OnThumbstick;
                    _Thumbstick.action.canceled -= OnThumbstick;
                }

                if (_Cancel != null)
                    _Cancel.action.performed -= OnCancelPerformed;
            }

            private void OnClickPerformed(InputAction.CallbackContext _) => Pressed?.Invoke(this);
            private void OnClickCanceled(InputAction.CallbackContext _) => Released?.Invoke(this);
            private void OnCancelPerformed(InputAction.CallbackContext _) => CancelPressed?.Invoke(this);
            private void OnThumbstick(InputAction.CallbackContext context) => ThumbstickChanged?.Invoke(this, context.ReadValue<Vector2>());

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
