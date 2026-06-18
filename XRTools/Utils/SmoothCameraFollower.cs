// Author: František Holubec
// Created: 18.02.2026

using DG.Tweening;
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
        [SerializeField]
        [Tooltip("Optional camera transform variable. If not set, will use Camera.main.transform")]
        private TransformScriptableVariable _CameraTransformVariable;

        [SerializeField]
        [Tooltip("Target transform to follow. If not set, will use this GameObject's transform")]
        private Transform _FollowTarget;
        
        [FormerlySerializedAs("_SpaceTransform")]
        [SerializeField]
        [Tooltip("Reference frame for the following")]
        private VariableField<Transform> _ReferenceFrame;
        
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
        [Tooltip("If the camera jumps further than this many meters (measured in the reference frame) in a single frame, snap the target to it instead of smoothing. Catches teleports. Set 0 to disable.")]
        [MinValue(0f)]
        [SuffixLabel("m", true)]
        private float _TeleportDistance = 1f;
        
        [SerializeField]
        [Tooltip("Follow camera pitch (X)")]
        private bool3 _FollowRotation = new(true, true, false);

        [SerializeField]
        [Tooltip("Angular deadzone in degrees per axis (pitch X / yaw Y / roll Z), max deviation per axis")]
        [SuffixLabel("°", true)]
        [MinValue(0f)]
        private Vector3 _AngularDeadzone;

        [SerializeField]
        [Tooltip("Positional deadzone half-extents in meters (max deviation per axis) along the panel's right/up/forward axes")]
        private Vector3 _PositionDeadzone;

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
        private bool _hasAnchorFrame;
        private Quaternion _anchorFrame = Quaternion.identity;
        private bool _hasAnchorPosition;
        private Vector3 _anchorLocalPosition;

        // The smoothed pose is persisted in the reference frame's local space so that motion of the
        // frame itself moves the target rigidly (no lag); only camera-relative motion is smoothed.
        private bool _hasLocalPose;
        private Vector3 _localPosition;
        private Quaternion _localRotation = Quaternion.identity;
        private bool _hasPrevCamLocalPosition;
        private Vector3 _prevCamLocalPosition;
        private Transform _prevReferenceFrame;

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
            var frame = spaceRotation * GetLocalCameraFrame(cam, spaceRotation);
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

            GetLocalTargetPose(cam, out var localPosition, out var localRotation, false);
            ResolveSpace(out var spacePosition, out var spaceRotation);
            var newPosition = spacePosition + spaceRotation * localPosition;
            var newRotation = spaceRotation * localRotation;

            // The world pose is about to change; force the smoothed local pose to re-seed from it.
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

            // If the reference frame itself was swapped for a different transform, every persisted value
            // (local pose, deadzone anchors, previous camera position) belongs to the old frame. Drop them
            // so they re-seed from the current world pose this frame - the target keeps its world pose
            // (no pop) and smoothly re-settles into the new frame instead of being smoothed across spaces.
            var referenceFrame = _ReferenceFrame?.Value;
            if (_hasLocalPose && !ReferenceEquals(referenceFrame, _prevReferenceFrame))
            {
                _hasLocalPose = false;
                _hasPrevCamLocalPosition = false;
                _hasAnchorFrame = false;
                _hasAnchorPosition = false;
            }
            _prevReferenceFrame = referenceFrame;

            // Detect a camera teleport relative to the reference frame (e.g. the rig is teleported).
            // Co-moving the whole frame (normal flight) leaves the local position unchanged, so it
            // never trips this; only a real jump relative to the frame does.
            var camLocalPosition = inverseSpaceRotation * (cam.position - spacePosition);
            var teleported = _TeleportDistance > 0f
                && _hasPrevCamLocalPosition
                && Vector3.Distance(camLocalPosition, _prevCamLocalPosition) > _TeleportDistance;
            _prevCamLocalPosition = camLocalPosition;
            _hasPrevCamLocalPosition = true;

            // On teleport, drop the deadzone anchors so the target re-seeds at the new pose with no trailing.
            if (teleported)
            {
                _hasAnchorFrame = false;
                _hasAnchorPosition = false;
            }

            GetLocalTargetPose(cam, out var localTargetPosition, out var localTargetRotation, true);

            var followTarget = FollowTarget;

            // Seed the persisted local pose from the current world pose so the first follow frame doesn't snap.
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
                // Smooth in the reference frame's local space. Because the smoothed pose is kept in
                // local space (not re-derived from the world transform), frame motion moves the target
                // rigidly and only camera-relative motion is smoothed - so it no longer lags the frame.
                _localPosition = Vector3.SmoothDamp(_localPosition, localTargetPosition, ref _positionVelocity, _PositionSmoothTime);
                _localRotation = RotationUtility.SmoothDampQuaternion(_localRotation, localTargetRotation, ref _rotationVelocity, _RotationSmoothTime);
            }

            followTarget.position = spacePosition + spaceRotation * _localPosition;
            followTarget.rotation = spaceRotation * _localRotation;
        }

        // Returns the target pose in the reference space's local coordinates (callers convert to world as needed).
        private void GetLocalTargetPose(Transform cam, out Vector3 localPosition, out Quaternion localRotation, bool applyDeadzone)
        {
            ResolveSpace(out var spacePosition, out var spaceRotation);
            var inverseSpaceRotation = Quaternion.Inverse(spaceRotation);

            // --- Rotation: per-axis angular deadzone (the anchor frame trails the camera within the deadzone) ---
            var desiredLocalFrame = GetLocalCameraFrame(cam, spaceRotation);
            Quaternion localFrame;
            if (!applyDeadzone || _AngularDeadzone == Vector3.zero || !_hasAnchorFrame)
            {
                localFrame = desiredLocalFrame;
            }
            else
            {
                // Measure the rotation from anchor to camera per axis and only let the part beyond the deadzone move the anchor.
                var deltaEuler = ToSignedEuler((Quaternion.Inverse(_anchorFrame) * desiredLocalFrame).eulerAngles);
                deltaEuler.x -= Mathf.Clamp(deltaEuler.x, -_AngularDeadzone.x, _AngularDeadzone.x);
                deltaEuler.y -= Mathf.Clamp(deltaEuler.y, -_AngularDeadzone.y, _AngularDeadzone.y);
                deltaEuler.z -= Mathf.Clamp(deltaEuler.z, -_AngularDeadzone.z, _AngularDeadzone.z);
                localFrame = _anchorFrame * Quaternion.Euler(deltaEuler);
            }
            _anchorFrame = localFrame;
            _hasAnchorFrame = true;

            var offsetPosition = _hasCustomPose ? _customPosePosition : _DefaultPosePosition;
            var offsetRotation = _hasCustomPose ? _customPoseRotation : Quaternion.Euler(_DefaultPoseRotation);

            // --- Position: box deadzone (the anchor trails the camera within the box), all in reference space ---
            var camLocalPosition = inverseSpaceRotation * (cam.position - spacePosition);
            var desiredLocalPosition = camLocalPosition + localFrame * offsetPosition;
            if (!applyDeadzone || !_hasAnchorPosition)
            {
                _anchorLocalPosition = desiredLocalPosition;
            }
            else
            {
                // Measure the deviation in the panel's local axes and only let the part beyond the box move the anchor.
                var localDelta = Quaternion.Inverse(localFrame) * (desiredLocalPosition - _anchorLocalPosition);
                localDelta.x -= Mathf.Clamp(localDelta.x, -_PositionDeadzone.x, _PositionDeadzone.x);
                localDelta.y -= Mathf.Clamp(localDelta.y, -_PositionDeadzone.y, _PositionDeadzone.y);
                localDelta.z -= Mathf.Clamp(localDelta.z, -_PositionDeadzone.z, _PositionDeadzone.z);
                _anchorLocalPosition += localFrame * localDelta;
            }
            _hasAnchorPosition = true;

            localPosition = _anchorLocalPosition;
            localRotation = localFrame * offsetRotation;
        }

        private void ResolveSpace(out Vector3 spacePosition, out Quaternion spaceRotation)
        {
            var space = _ReferenceFrame?.Value;
            spacePosition = space != null ? space.position : Vector3.zero;
            spaceRotation = space != null ? space.rotation : Quaternion.identity;
        }

        // Converts a [0..360) euler into signed [-180..180) components so per-axis clamping works symmetrically.
        private static Vector3 ToSignedEuler(Vector3 euler) => new(
            Mathf.DeltaAngle(0f, euler.x),
            Mathf.DeltaAngle(0f, euler.y),
            Mathf.DeltaAngle(0f, euler.z));

        // Camera orientation relative to the reference space (aircraft), with the unfollowed axes filtered out.
        // Filtering happens in this local frame so the space's own rotation never eats into the deadzone.
        private Quaternion GetLocalCameraFrame(Transform cam, Quaternion spaceRotation)
        {
            var localCamRotation = Quaternion.Inverse(spaceRotation) * cam.rotation;

            if (_FollowRotation.x && _FollowRotation.y && _FollowRotation.z)
                return localCamRotation;

            if (!_FollowRotation.x && !_FollowRotation.y && !_FollowRotation.z)
                return Quaternion.identity;

            var euler = localCamRotation.eulerAngles;
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
                // Position deadzone box: how far the camera can move (in the panel's right/up/forward axes) before the target follows.
                var prevMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(camPosition, camFrame, Vector3.one);
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
                Gizmos.DrawWireCube(Vector3.zero, _PositionDeadzone * 2f);
                Gizmos.matrix = prevMatrix;
            }

            if (_AngularDeadzone != Vector3.zero)
            {
                // Per-axis angular deadzone: how far the camera can rotate around each axis before the target follows.
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
