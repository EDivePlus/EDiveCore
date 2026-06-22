// Author: František Holubec
// Created: 18.02.2026

using System;
using DG.Tweening;
using EDIVE.Conditions;
using EDIVE.DataStructures.VariableFields;
using EDIVE.NativeUtils;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.XRTools
{
    public class SmoothCameraFollower : MonoBehaviour
    {
        private enum FollowMode
        {
            Manual,
            Automatic
        }

        [SerializeField]
        [Tooltip("Camera to follow. Defaults to Camera.main")]
        private TransformScriptableVariable _CameraTransformVariable;

        [SerializeField]
        [Tooltip("Object to move. Defaults to this transform")]
        private Transform _FollowTarget;

        [FormerlySerializedAs("_SpaceTransform")]
        [SerializeField]
        [Tooltip("Reference frame")]
        private VariableField<Transform> _ReferenceFrame;

        [PropertySpace]
        [SerializeField]
        [Tooltip("Face away from camera. Off = parallel to view plane")]
        private bool _UseLookRotation = true;
        
        [SerializeField]
        [FormerlySerializedAs("_PositionOffset")]
        [Tooltip("Offset from camera (right/up/forward)")]
        private Vector3 _DefaultPosePosition = new(0f, 0f, 1f);

        [SerializeField]
        [Tooltip("Rotation offset (euler). 0 = faces away from camera")]
        private Vector3 _DefaultPoseRotation;
        
        [SerializeField]
        [Tooltip("On follow, keep current pose instead of default")]
        private bool _SetCustomPoseOnFollow;
     
        [SerializeReference]
        [Tooltip("Capture current pose")]
        private IActivation _SetCustomPoseActivation;
        
        [SerializeReference]
        [Tooltip("Clear captured pose, return to default")]
        private IActivation _ResetCustomPoseActivation;

        [SerializeField]
        [Tooltip("Reposition tween duration")]
        private float _RepositionDuration = 0.3f;
        
        [SerializeField]
        private bool _RepositionOnAwake = true;
        
        [SerializeReference]
        private IActivation _RepositionAction;
        
        [PropertySpace]
        [SerializeField]
        [Tooltip("Follow pitch (X), yaw (Y), roll (Z). Off axes stay upright")]
        private bool3 _FollowRotation = new(true, true, false);
        
        [SerializeField]
        private float _PositionSmoothTime = 0.15f;

        [SerializeField]
        private float _RotationSmoothTime = 0.1f;

        [SerializeField]
        [Tooltip("Snap instead of smoothing if camera jumps more than this (m)")]
        [MinValue(0f)]
        private float _FollowTeleportDistance = 1f;

        [SerializeField]
        [Tooltip("Angular deadzone per axis (pitch/yaw/roll)")]
        [SuffixLabel("°", true)]
        [MinValue(0f)]
        private Vector3 _AngularDeadzone;

        [SerializeField]
        [Tooltip("Position deadzone half-extents (camera right/up/forward)")]
        private Vector3 _PositionDeadzone;

        [SerializeField]
        private FollowMode _FollowMode = FollowMode.Manual;

        [ShowIf(nameof(_FollowMode), FollowMode.Manual)]
        [SerializeField]
        private bool _FollowOnAwake;
        
        [ShowIf(nameof(_FollowMode), FollowMode.Manual)]
        [SerializeReference]
        private IActivation _ToggleFollowActivation;
        
        [ShowIf(nameof(_FollowMode), FollowMode.Automatic)]
        [SerializeReference]
        private ICondition _AutoFollowCondition;

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
        private bool _hasAnchorFrame;
        private Quaternion _anchorFrame = Quaternion.identity;
        private bool _hasAnchorPosition;
        private Vector3 _anchorLocalPosition;
        
        private bool _hasLocalPose;
        private Vector3 _localPosition;
        private Quaternion _localRotation = Quaternion.identity;
        private bool _hasPrevCamLocalPosition;
        private Vector3 _prevCamLocalPosition;
        private Transform _prevReferenceFrame;
        private bool _started;

        private void OnEnable()
        {
            _RepositionAction?.RegisterActivationListener(Reposition);
            _SetCustomPoseActivation?.RegisterActivationListener(SetCustomPose);
            _ResetCustomPoseActivation?.RegisterActivationListener(ResetCustomPose);
            switch (_FollowMode)
            {
                case FollowMode.Manual: _ToggleFollowActivation?.RegisterActivationListener(ToggleFollow); 
                    break;
                case FollowMode.Automatic:
                    if (_AutoFollowCondition != null)
                    {
                        _AutoFollowCondition.InitializeObserving();
                        _AutoFollowCondition.StateChanged += OnAutoFollowConditionChanged;
                    }
                    if (_started)
                        OnAutoFollowConditionChanged();
                    break;
                default: 
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDisable()
        {
            _RepositionAction?.UnregisterActivationListener(Reposition);
            _SetCustomPoseActivation?.UnregisterActivationListener(SetCustomPose);
            _ResetCustomPoseActivation?.UnregisterActivationListener(ResetCustomPose);
            _ToggleFollowActivation?.UnregisterActivationListener(ToggleFollow);
            if (_AutoFollowCondition != null)
            {
                _AutoFollowCondition.StateChanged -= OnAutoFollowConditionChanged;
                _AutoFollowCondition.TerminateObserving();
            }
        }

        private void Start()
        {
            _started = true;

            if (_RepositionOnAwake)
                Reposition(true);

            switch (_FollowMode)
            {
                case FollowMode.Manual:
                    if (_FollowOnAwake)
                        SetFollowing(true);
                    break;
                case FollowMode.Automatic:
                    OnAutoFollowConditionChanged();
                    break;
            }
        }

        private void OnAutoFollowConditionChanged()
        {
            SetFollowing(_AutoFollowCondition != null && _AutoFollowCondition.Evaluate());
        }

        private void LateUpdate()
        {
            // the reposition tween drives the world pose directly
            if (_repositionTween.IsActive() && _repositionTween.IsPlaying())
                return;

            if (_isFollowing)
                FollowCamera();
            else
                HoldInReferenceFrame();
        }

        public void SetFollowing(bool following)
        {
            _isFollowing = following;
            if (following)
            {
                _hasAnchorFrame = false;
                _hasAnchorPosition = false;
                _hasLocalPose = false;
                _hasPrevCamLocalPosition = false;
                if (_SetCustomPoseOnFollow)
                    SetCustomPose();
            }

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

            ResolveSpace(out _, out var spaceRotation);
            var inverseSpaceRotation = Quaternion.Inverse(spaceRotation);
            var followTarget = FollowTarget;

            var placementFrame = spaceRotation * GetLocalCameraFrame(cam, spaceRotation);
            _customPosePosition = Quaternion.Inverse(placementFrame) * (followTarget.position - cam.position);

            var baseFrame = placementFrame;
            if (_UseLookRotation)
            {
                var toPanel = inverseSpaceRotation * (followTarget.position - cam.position);
                if (toPanel.sqrMagnitude > 1e-6f)
                    baseFrame = spaceRotation * MaskFollowedAxes(Quaternion.LookRotation(toPanel, Vector3.up));
            }
            _customPoseRotation = Quaternion.Inverse(baseFrame) * followTarget.rotation;
            _hasCustomPose = true;
        }


        [Button]
        public void ResetCustomPose()
        {
            _hasCustomPose = false;
        }
        
        public void Reposition() => Reposition(false);
        
        [Button]
        public void Reposition(bool immediate)
        {
            var cam = CameraTransform;
            if (cam == null)
                return;

            GetLocalTargetPose(cam, out var localPosition, out var localRotation, false);
            ResolveSpace(out var spacePosition, out var spaceRotation);
            var newPosition = spacePosition + spaceRotation * localPosition;
            var newRotation = spaceRotation * localRotation;

            // re-seed local pose from new world pose
            _hasLocalPose = false;
            _hasPrevCamLocalPosition = false;

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

            ResolveSpace(out var spacePosition, out var spaceRotation);
            var inverseSpaceRotation = Quaternion.Inverse(spaceRotation);

            // reference frame changed: drop stale local state
            var referenceFrame = _ReferenceFrame?.Value;
            if (_hasLocalPose && !ReferenceEquals(referenceFrame, _prevReferenceFrame))
            {
                _hasLocalPose = false;
                _hasPrevCamLocalPosition = false;
                _hasAnchorFrame = false;
                _hasAnchorPosition = false;
            }
            _prevReferenceFrame = referenceFrame;

            // teleport check (in local space, so frame motion doesn't trip it)
            var camLocalPosition = inverseSpaceRotation * (cam.position - spacePosition);
            var teleported = _FollowTeleportDistance > 0f
                && _hasPrevCamLocalPosition
                && Vector3.Distance(camLocalPosition, _prevCamLocalPosition) > _FollowTeleportDistance;
            _prevCamLocalPosition = camLocalPosition;
            _hasPrevCamLocalPosition = true;

            if (teleported)
            {
                _hasAnchorFrame = false;
                _hasAnchorPosition = false;
            }

            GetLocalTargetPose(cam, out var localTargetPosition, out var localTargetRotation, true);

            var followTarget = FollowTarget;

            // seed local pose so the first frame doesn't snap
            if (!_hasLocalPose)
            {
                _localPosition = inverseSpaceRotation * (followTarget.position - spacePosition);
                _localRotation = inverseSpaceRotation * followTarget.rotation;
                _positionVelocity = Vector3.zero;
                _rotationVelocity = Vector3.zero;
                _hasLocalPose = true;
            }

            if (teleported)
            {
                _localPosition = localTargetPosition;
                _localRotation = localTargetRotation;
                _positionVelocity = Vector3.zero;
                _rotationVelocity = Vector3.zero;
            }
            else
            {
                // smooth in local space so only camera motion smooths, not frame motion
                _localPosition = Vector3.SmoothDamp(_localPosition, localTargetPosition, ref _positionVelocity, _PositionSmoothTime);
                _localRotation = RotationUtility.SmoothDampQuaternion(_localRotation, localTargetRotation, ref _rotationVelocity, _RotationSmoothTime);
            }

            followTarget.position = spacePosition + spaceRotation * _localPosition;
            followTarget.rotation = spaceRotation * _localRotation;
        }

        // keep the target pinned to its local pose within the reference frame while not following,
        // so frame motion still carries it
        private void HoldInReferenceFrame()
        {
            ResolveSpace(out var spacePosition, out var spaceRotation);
            var inverseSpaceRotation = Quaternion.Inverse(spaceRotation);

            // reference frame changed: re-capture local pose from the current world pose
            var referenceFrame = _ReferenceFrame?.Value;
            if (_hasLocalPose && !ReferenceEquals(referenceFrame, _prevReferenceFrame))
                _hasLocalPose = false;
            _prevReferenceFrame = referenceFrame;

            var followTarget = FollowTarget;

            // capture the current world pose as the local pose to hold
            if (!_hasLocalPose)
            {
                _localPosition = inverseSpaceRotation * (followTarget.position - spacePosition);
                _localRotation = inverseSpaceRotation * followTarget.rotation;
                _positionVelocity = Vector3.zero;
                _rotationVelocity = Vector3.zero;
                _hasLocalPose = true;
            }

            followTarget.position = spacePosition + spaceRotation * _localPosition;
            followTarget.rotation = spaceRotation * _localRotation;
        }

        // target pose in reference-frame local space
        private void GetLocalTargetPose(Transform cam, out Vector3 localPosition, out Quaternion localRotation, bool applyDeadzone)
        {
            ResolveSpace(out var spacePosition, out var spaceRotation);
            var inverseSpaceRotation = Quaternion.Inverse(spaceRotation);

            // deadzone the camera pose first; rotation is derived from it below
            var camFrame = ApplyAngularDeadzone(GetLocalCameraFrame(cam, spaceRotation), applyDeadzone);
            var camPosition = inverseSpaceRotation * (cam.position - spacePosition);
            camPosition = ApplyPositionDeadzone(camPosition, camFrame, applyDeadzone);

            var offsetPosition = _hasCustomPose ? _customPosePosition : _DefaultPosePosition;
            var offsetRotation = _hasCustomPose ? _customPoseRotation : Quaternion.Euler(_DefaultPoseRotation);

            localPosition = camPosition + camFrame * offsetPosition;

            var baseFrame = camFrame;
            if (_UseLookRotation)
            {
                var toPanel = localPosition - camPosition; // == camFrame * offsetPosition
                if (toPanel.sqrMagnitude > 1e-6f)
                    baseFrame = MaskFollowedAxes(Quaternion.LookRotation(toPanel, Vector3.up));
            }
            localRotation = baseFrame * offsetRotation;
        }

        // only rotation beyond the threshold moves the anchor
        private Quaternion ApplyAngularDeadzone(Quaternion desiredFrame, bool applyDeadzone)
        {
            Quaternion frame;
            if (!applyDeadzone || _AngularDeadzone == Vector3.zero || !_hasAnchorFrame)
            {
                frame = desiredFrame;
            }
            else
            {
                var deltaEuler = ToSignedEuler((Quaternion.Inverse(_anchorFrame) * desiredFrame).eulerAngles);
                deltaEuler.x -= Mathf.Clamp(deltaEuler.x, -_AngularDeadzone.x, _AngularDeadzone.x);
                deltaEuler.y -= Mathf.Clamp(deltaEuler.y, -_AngularDeadzone.y, _AngularDeadzone.y);
                deltaEuler.z -= Mathf.Clamp(deltaEuler.z, -_AngularDeadzone.z, _AngularDeadzone.z);
                frame = _anchorFrame * Quaternion.Euler(deltaEuler);
            }
            _anchorFrame = frame;
            _hasAnchorFrame = true;
            return frame;
        }

        // box deadzone around the camera position, in camera-frame axes
        private Vector3 ApplyPositionDeadzone(Vector3 desiredPosition, Quaternion frame, bool applyDeadzone)
        {
            if (!applyDeadzone || !_hasAnchorPosition)
            {
                _anchorLocalPosition = desiredPosition;
            }
            else
            {
                var localDelta = Quaternion.Inverse(frame) * (desiredPosition - _anchorLocalPosition);
                localDelta.x -= Mathf.Clamp(localDelta.x, -_PositionDeadzone.x, _PositionDeadzone.x);
                localDelta.y -= Mathf.Clamp(localDelta.y, -_PositionDeadzone.y, _PositionDeadzone.y);
                localDelta.z -= Mathf.Clamp(localDelta.z, -_PositionDeadzone.z, _PositionDeadzone.z);
                _anchorLocalPosition += frame * localDelta;
            }
            _hasAnchorPosition = true;
            return _anchorLocalPosition;
        }

        private void ResolveSpace(out Vector3 spacePosition, out Quaternion spaceRotation)
        {
            var space = _ReferenceFrame?.Value;
            spacePosition = space != null ? space.position : Vector3.zero;
            spaceRotation = space != null ? space.rotation : Quaternion.identity;
        }
        
        private static Vector3 ToSignedEuler(Vector3 euler) => new(
            Mathf.DeltaAngle(0f, euler.x),
            Mathf.DeltaAngle(0f, euler.y),
            Mathf.DeltaAngle(0f, euler.z));
        
        private Quaternion GetLocalCameraFrame(Transform cam, Quaternion spaceRotation)
        {
            return MaskFollowedAxes(Quaternion.Inverse(spaceRotation) * cam.rotation);
        }
        
        private Quaternion MaskFollowedAxes(Quaternion localRotation)
        {
            var euler = localRotation.eulerAngles;
            return Quaternion.Euler(
                _FollowRotation.x ? euler.x : 0f,
                _FollowRotation.y ? euler.y : 0f,
                _FollowRotation.z ? euler.z : 0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var target = FollowTarget;
            if (target == null)
                return;
            
            var camFrame = target.rotation * Quaternion.Inverse(Quaternion.Euler(_DefaultPoseRotation));
            var camPosition = target.position - camFrame * _DefaultPosePosition;

            const float radius = 0.05f;
            var forward = camFrame * Vector3.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(camPosition, radius);
            Gizmos.DrawLine(camPosition, camPosition + forward * (target.position - camPosition).magnitude);

            if (_PositionDeadzone != Vector3.zero)
            {
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(camPosition, camFrame, Vector3.one);
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
                Gizmos.DrawWireCube(Vector3.zero, _PositionDeadzone * 2f);
                Gizmos.matrix = prevMatrix;
            }

            if (_AngularDeadzone != Vector3.zero)
            {
                var distance = (target.position - camPosition).magnitude;
                var right = camFrame * Vector3.right;
                var up = camFrame * Vector3.up;
                var panelCenter = camPosition + forward * distance;

                UnityEditor.Handles.color = new Color(1f, 0.6f, 0f, 0.9f);
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.7f);
                DrawDeadzoneArc(camPosition, up, forward, _AngularDeadzone.y, distance);          // yaw
                DrawDeadzoneArc(camPosition, right, forward, _AngularDeadzone.x, distance);        // pitch
                DrawDeadzoneArc(panelCenter, forward, up, _AngularDeadzone.z, distance * 0.3f);    // roll
            }
        }

        private static void DrawDeadzoneArc(Vector3 center, Vector3 axis, Vector3 zeroDir, float halfAngle, float radius)
        {
            if (halfAngle <= 0f)
                return;

            var from = Quaternion.AngleAxis(-halfAngle, axis) * zeroDir;
            var to = Quaternion.AngleAxis(halfAngle, axis) * zeroDir;
            UnityEditor.Handles.DrawWireArc(center, axis, from, halfAngle * 2f, radius);
            Gizmos.DrawLine(center, center + from * radius);
            Gizmos.DrawLine(center, center + to * radius);
        }
#endif
    }
}
