// Author: František Holubec
// Created: 26.06.2025

using DG.Tweening;
using EDIVE.Core.Services;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using EDIVE.XRTools.Keyboard;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace EDIVE.XRTools.Tablet
{
    public class TabletController : AServiceBehaviour<TabletController>
    {
        [SerializeField]
        private KeyboardController _Keyboard;

        [SerializeField]
        private Canvas _MainLayer;

        [SerializeField]
        private Canvas _OverlayLayer;

        [SerializeField]
        private Canvas _DebugLayer;

        [SerializeField]
        private TransformScriptableVariable _CameraTransformVariable;

        [SerializeField]
        private InputActionReference _RepositionAction;

        [SerializeField]
        private Vector3 _PositionOffset;

        [SerializeField]
        [Range(0, 1)]
        private float _FacingThreshold = 0.3f;

        [SerializeField]
        [Range(0, 1)]
        private float _FrontThreshold = 0.2f;

        [SerializeField]
        private float _MaxDistance = 1f;

        [SerializeField]
        private float _TweenDuration = 0.3f;

        public KeyboardController Keyboard => _Keyboard;
        public Canvas MainLayer => _MainLayer;
        public Canvas OverlayLayer => _OverlayLayer;
        public Canvas DebugLayer => _DebugLayer;

        private Transform CameraTransform => _CameraTransformVariable != null && _CameraTransformVariable.Value != null
            ? _CameraTransformVariable.Value
            : Camera.main?.transform;

        [ShowInInspector]
        public bool IsInView => CheckInView();

        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }

        private bool _isOpen;
        private Tween _animTween;

        protected override void Awake()
        {
            base.Awake();
            _isOpen = true;
            if (_RepositionAction)
                _RepositionAction.action.performed += OnRepositionPerformed;
        }

        private void OnRepositionPerformed(InputAction.CallbackContext context)
        {
            ToggleTablet();
        }

        [Button]
        public void ToggleTablet()
        {
            var isInView = CheckInView();
            if (isInView && IsOpen)
            {
                SetOpen(false);
            }
            else
            {
                SetOpen(true);
                RepositionTablet();
            }
        }

        [Button]
        public void RepositionTablet()
        {
            SetOpen(true);
        }

        public void SetOpen(bool open)
        {
            _animTween?.Kill();
            _isOpen = open;
            var newScale = open ? Vector3.one : Vector3.zero;

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad));

            if (open)
            {
                var target = CameraTransform;
                if (target == null)
                    return;

                var newPosition = target.position +
                                  target.right * _PositionOffset.x +
                                  target.forward * _PositionOffset.z +
                                  Vector3.up * _PositionOffset.y;

                var forward = (newPosition - target.position).normalized;
                BurstMathUtility.OrthogonalLookRotation(forward, Vector3.up, out var newRotation);

                sequence.Join(transform.DOMove(newPosition, _TweenDuration).SetEase(Ease.InOutQuad));
                sequence.Join(transform.DORotateQuaternion(newRotation, _TweenDuration).SetEase(Ease.InOutQuad));
            }
            _animTween = sequence;
        }

        private bool CheckInView()
        {
            var target = CameraTransform;
            if (target == null)
                return false;

            return XRUtils.IsTargetInView(target, transform, _MaxDistance, _FacingThreshold, _FacingThreshold);
        }
    }
}
