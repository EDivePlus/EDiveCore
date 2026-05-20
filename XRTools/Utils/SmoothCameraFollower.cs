// Author: František Holubec
// Created: 18.02.2026

using DG.Tweening;
using EDIVE.NativeUtils;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        [PropertySpace]
        [SerializeField]
        [FormerlySerializedAs("_PositionOffset")]
        [Tooltip("Default position offset relative to the constrained camera frame (right/up/forward)")]
        private Vector3 _DefaultPosePosition = new(0f, 0f, 1f);

        [SerializeField]
        [Tooltip("Default rotation offset (euler) relative to the constrained camera frame. (0,0,0) faces away from the camera.")]
        private Vector3 _DefaultPoseRotation;
        
        [SerializeField]
        [Tooltip("Duration of the tween used by Reposition() / ResetCustomPose()")]
        private float _RepositionDuration = 0.3f;
        
        [SerializeField]
        private bool _RepositionOnAwake = true;
        
        [PropertySpace]
        [SerializeField]
        private float _PositionSmoothTime = 0.15f;

        [SerializeField]
        private float _RotationSmoothTime = 0.1f;

        [SerializeField]
        [Tooltip("Follow the camera's Pitch (X). Enable to let the target tilt / move up & down as you look up & down.")]
        private bool _FollowRotationX = true;

        [SerializeField]
        [Tooltip("Follow the camera's Yaw (Y). Enable to let the target rotate / orbit horizontally as you turn. This is the usual one.")]
        private bool _FollowRotationY = true;

        [SerializeField]
        [Tooltip("Follow the camera's Roll (Z). Usually OFF so the target always stays horizontal.")]
        private bool _FollowRotationZ;
        
        [SerializeField]
        private bool _FollowOnAwake;
        
        [SerializeField]
        [Tooltip("When following is turned on, automatically capture the target's current pose so it stays where it currently is.")]
        private bool _SetCustomPoseOnFollow;

        [PropertySpace]
        [SerializeReference]
        private IActivation _ToggleFollowActivation;

        [SerializeReference]
        [Tooltip("Captures the current pose relative to the camera (target stays where it is while following)")]
        private IActivation _SetCustomPoseActivation;

        [SerializeReference]
        [Tooltip("Clears the captured pose and moves the target back to the default pose")]
        private IActivation _ResetCustomPoseActivation;

        [SerializeField]
        private AToggleState _FollowState;

        
        [PropertySpace]
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
        private bool _hasCustomPose;
        private Vector3 _customPosePosition;
        private Quaternion _customPoseRotation = Quaternion.identity;
        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;
        private Tween _repositionTween;

        private void OnEnable()
        {
            _ToggleFollowActivation?.RegisterActivationListener(ToggleFollow);
            _SetCustomPoseActivation?.RegisterActivationListener(SetCustomPose);
            _ResetCustomPoseActivation?.RegisterActivationListener(ResetCustomPose);
        }

        private void OnDisable()
        {
            _ToggleFollowActivation?.UnregisterActivationListener(ToggleFollow);
            _SetCustomPoseActivation?.UnregisterActivationListener(SetCustomPose);
            _ResetCustomPoseActivation?.UnregisterActivationListener(ResetCustomPose);
        }

        private void Start()
        {
            if (_RepositionOnAwake)
                Reposition(true);

            if (_FollowOnAwake)
                SetFollowing(true);
        }

        private void LateUpdate()
        {
            if (_isFollowing && (!_repositionTween.IsActive() || !_repositionTween.IsPlaying()))
                FollowCamera();
        }

        public void SetFollowing(bool following)
        {
            _isFollowing = following;
            if (following && _SetCustomPoseOnFollow)
                SetCustomPose();

            if(_FollowState)
                _FollowState.SetState(_isFollowing);
        }

        private void ToggleFollow()
        {
            SetFollowing(!IsFollowing);
        }
        
        [Button]
        public void SetCustomPose()
        {
            var cam = CameraTransform;
            if (cam == null)
                return;

            var frame = GetCameraFrame(cam);
            var followTarget = FollowTarget;
            var inverse = Quaternion.Inverse(frame);
            _customPosePosition = inverse * (followTarget.position - cam.position);
            _customPoseRotation = inverse * followTarget.rotation;
            _hasCustomPose = true;
        }


        [Button]
        public void ResetCustomPose()
        {
            _hasCustomPose = false;
        }
        
        [Button]
        public void Reposition(bool immediate = false)
        {
            var cam = CameraTransform;
            if (cam == null)
                return;

            GetTargetPose(cam, out var newPosition, out var newRotation);

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

        private void FollowCamera()
        {
            var cam = CameraTransform;
            if (cam == null)
                return;

            GetTargetPose(cam, out var targetPosition, out var targetRotation);

            var followTarget = FollowTarget;
            followTarget.position = Vector3.SmoothDamp(followTarget.position, targetPosition, ref _positionVelocity, _PositionSmoothTime);
            followTarget.rotation = RotationUtility.SmoothDampQuaternion(followTarget.rotation, targetRotation, ref _rotationVelocity, _RotationSmoothTime);
        }

        private void GetTargetPose(Transform cam, out Vector3 position, out Quaternion rotation)
        {
            var frame = GetCameraFrame(cam);
            if (_hasCustomPose)
            {
                position = cam.position + frame * _customPosePosition;
                rotation = frame * _customPoseRotation;
            }
            else
            {
                position = cam.position + frame * _DefaultPosePosition;
                rotation = frame * Quaternion.Euler(_DefaultPoseRotation);
            }
        }
        
        private Quaternion GetCameraFrame(Transform cam)
        {
            if (_FollowRotationX && _FollowRotationY && _FollowRotationZ)
                return cam.rotation;

            if (!_FollowRotationX && !_FollowRotationY && !_FollowRotationZ)
                return Quaternion.identity;

            var euler = cam.eulerAngles;
            return Quaternion.Euler(
                _FollowRotationX ? euler.x : 0f,
                _FollowRotationY ? euler.y : 0f,
                _FollowRotationZ ? euler.z : 0f);
        }
    }
}
