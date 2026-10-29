// Author: František Holubec
// Created: 26.06.2025

using DG.Tweening;
using EDIVE.Core.Services;
using EDIVE.Input.Keyboard;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.XRTools.Tablet
{
    public class TabletController : AServiceBehaviour<TabletController>
    {
        [SerializeField]
        private SmoothCameraFollower _CameraFollower;

        [FormerlySerializedAs("_RepositionAction")]
        [SerializeReference]
        private IActivation _FocusAction;
        
        [SerializeField]
        [Range(0, 1)]
        private float _FacingThreshold = 0.3f;

        [SerializeField]
        [Range(0, 1)]
        private float _FrontThreshold = 0.2f;

        [SerializeField]
        private float _MaxDistance = 1f;

        [SerializeField]
        private float _InitialScale = 1;

        [SerializeField]
        [Tooltip("Camera jump that counts as a teleport, the tablet comes along when open and in view (m)")]
        [MinValue(0f)]
        private float _TeleportDistance = 1f;
        
        [SerializeField]
        private float _TweenDuration = 0.3f;

        [ShowInInspector]
        public bool IsInView => CheckInView();

        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }

        private bool _isOpen;
        private float _openScale;
        private Tween _animTween;
        private bool _hasCameraSample;
        private Vector3 _lastCameraPosition;
        private float _lastCameraYaw;
        private bool _wasInView;

        protected override void OnEnable()
        {
            base.OnEnable();
            _FocusAction?.Register(ToggleTablet);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _FocusAction?.Unregister(ToggleTablet);
            _animTween?.Kill();
        }

        private void Awake()
        {
            _isOpen = true;
            _openScale = _InitialScale;
            transform.localScale = Vector3.one * _InitialScale;
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
            if (_CameraFollower != null) 
                _CameraFollower.Reposition();
        }

        public void SetOpen(bool open, bool immediate = false)
        {
            // Cache open scale
            if (_isOpen && !_animTween.IsActive()) 
                _openScale = transform.localScale.x;
            
            _animTween?.Kill();
            _isOpen = open;
            
            if (_CameraFollower != null)
                _CameraFollower.Reposition(immediate);

            var newScale = open ? _openScale * Vector3.one : Vector3.zero;

            if (immediate)
                transform.localScale = newScale;
            else
                _animTween = transform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
        }

        // After the rig moved this frame, teleports included
        private void LateUpdate()
        {
            var cam = _CameraFollower != null ? _CameraFollower.CameraTransform : null;
            if (cam == null)
            {
                _hasCameraSample = false;
                return;
            }

            var position = cam.position;
            var yaw = cam.eulerAngles.y;
            // A following tablet snaps on its own
            if (_hasCameraSample && _wasInView && !_CameraFollower.IsFollowing && Vector3.Distance(position, _lastCameraPosition) > _TeleportDistance)
            {
                var turn = Quaternion.Euler(0f, yaw - _lastCameraYaw, 0f);
                transform.SetPositionAndRotation(position + turn * (transform.position - _lastCameraPosition), turn * transform.rotation);
            }

            _lastCameraPosition = position;
            _lastCameraYaw = yaw;
            _wasInView = IsOpen && CheckInView();
            _hasCameraSample = true;
        }

        private bool CheckInView()
        {
            if(_CameraFollower == null) 
                return false;
            
            var target = _CameraFollower.CameraTransform;
            return target != null && XRUtils.IsTargetInView(target, transform, _MaxDistance, _FrontThreshold, _FacingThreshold);
        }
    }
}
