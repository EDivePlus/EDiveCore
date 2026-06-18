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
        private Tween _animTween;

        protected override void OnEnable()
        {
            base.OnEnable();
            _FocusAction?.RegisterActivationListener(ToggleTablet);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _FocusAction?.UnregisterActivationListener(ToggleTablet);
            _animTween?.Kill();
        }

        private void Awake()
        {
            _isOpen = true;
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

        public void SetOpen(bool open)
        {
            _animTween?.Kill();
            _isOpen = open;
            
            if (_CameraFollower != null)
                _CameraFollower.Reposition();
            
            var newScale = open ? Vector3.one : Vector3.zero;
            _animTween = transform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
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
