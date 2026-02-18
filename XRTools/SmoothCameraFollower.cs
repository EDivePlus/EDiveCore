// Author: František Holubec
// Created: 18.02.2026

using System;
using DG.Tweening;
using EDIVE.NativeUtils;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace EDIVE.XRTools
{
    public class SmoothCameraFollower : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Optional camera transform variable. If not set, will use Camera.main.transform")]
        private TransformScriptableVariable _CameraTransformVariable;
        
        [SerializeField]
        [Tooltip("Target transform to follow. If not set, will use this GameObject's transform")]
        private Transform _FollowTarget;
        
        [SerializeField]
        private Vector3 _PositionOffset = new(0f, 0f, 1f);
        
        [SerializeField]
        private float _RepositionDuration = 0.3f;
        
        [SerializeField]
        private float _PositionSmoothTime = 0.15f;
        
        [SerializeField]
        private float _RotationSmoothTime = 0.1f;
        
        [SerializeField]
        private bool _RepositionOnAwake = true;
        
        [SerializeField]
        private bool _FollowOnAwake;
        
        [SerializeReference]
        private IActivation _ToggleFollowActivation;

        [SerializeField]
        private AToggleState _FollowState;
        
        [ShowInInspector]
        [ReadOnly]
        public bool IsFollowing
        {
            get => _isFollowing;
            set => SetFollowing(value);
        }
        
        public Transform CameraTransform => _CameraTransformVariable != null && _CameraTransformVariable.Value != null
            ? _CameraTransformVariable.Value
            : Camera.main?.transform;
        
        private Transform FollowTarget => _FollowTarget != null ? _FollowTarget : transform;
        
        private bool _isFollowing;
        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;
        private Tween _repositionTween;
        
        protected void Awake()
        {
            if (_RepositionOnAwake)
                Reposition(true);
            
            if (_FollowOnAwake)
                SetFollowing(true);
            
            _ToggleFollowActivation?.RegisterActivationListener(ToggleFollow);
        }

        private void OnDestroy()
        {
            _ToggleFollowActivation?.UnregisterActivationListener(ToggleFollow);
        }

        private void LateUpdate()
        {
            if (_isFollowing && !_repositionTween.IsPlaying()) 
                FollowCamera();
        }
        
        public void SetFollowing(bool following)
        {
            _isFollowing = following;
            if(_FollowState)
                _FollowState.SetState(_isFollowing);
        }
        
        private void ToggleFollow()
        {
            SetFollowing(!IsFollowing);
        }
        
        private void FollowCamera()
        {
            var target = CameraTransform;
            if (target == null) 
                return;
            
            var targetPosition = CalculateTargetPosition(target);
            var targetRotation = CalculateTargetRotation(target, targetPosition);
            
            var followTarget = FollowTarget;
            followTarget.position = Vector3.SmoothDamp(followTarget.position, targetPosition, ref _positionVelocity, _PositionSmoothTime);
            followTarget.rotation = RotationUtility.SmoothDampQuaternion(followTarget.rotation, targetRotation, ref _rotationVelocity, _RotationSmoothTime);
        }
        
        [Button]
        public void Reposition(bool immediate = false)
        {
            var target = CameraTransform;
            if (target == null)
                return;
            
            var newPosition = CalculateTargetPosition(target);
            var newRotation = CalculateTargetRotation(target, newPosition);
            
            var followTarget = FollowTarget;
            _repositionTween?.Kill();
            if (immediate)
            {
                followTarget.position = newPosition;
                followTarget.rotation = newRotation;
            }
            else
            {
                _repositionTween = DOTween.Sequence()
                    .Append(followTarget.DOMove(newPosition, _RepositionDuration).SetEase(Ease.InOutQuad))
                    .Join(followTarget.DORotateQuaternion(newRotation, _RepositionDuration).SetEase(Ease.InOutQuad));
            }
            
        }

        private Vector3 CalculateTargetPosition(Transform cameraTransform)
        {
            return cameraTransform.position + 
                   cameraTransform.right * _PositionOffset.x +
                   cameraTransform.forward * _PositionOffset.z +
                   Vector3.up * _PositionOffset.y;
        }
        
        private Quaternion CalculateTargetRotation(Transform cameraTransform, Vector3 targetPosition)
        {
            var forward = (targetPosition - cameraTransform.position).normalized;
            BurstMathUtility.OrthogonalLookRotation(forward, Vector3.up, out var rotation);
            return rotation;
        }
    }
}

