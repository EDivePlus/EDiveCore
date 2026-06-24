// Author: František Holubec
// Created: 12.06.2026

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Avatars
{
    [DefaultExecutionOrder(100)]
    public class VRBodySolver : MonoBehaviour
    {
        [EnhancedFoldoutGroup("Animator", "@ColorTools.Red", SpaceAfter = 4)]
        [ShowInFoldoutHeader]
        [Required]
        [HideLabel]
        [SerializeField]
        private Animator _Animator;

        [EnhancedFoldoutGroup("Animator")]
        [SerializeField]
        [Tooltip("Drive the animator params below.")]
        private bool _DriveAnimatorParams = true;

        [EnhancedFoldoutGroup("Animator")]
        [ShowIf(nameof(_DriveAnimatorParams))]
        [Tooltip("Sideways velocity param. Empty = off.")]
        [AnimatorParameter(nameof(_Animator), AnimatorControllerParameterType.Float)]
        [SerializeField]
        private string _HorizontalParam = "Horizontal";

        [EnhancedFoldoutGroup("Animator")]
        [ShowIf(nameof(_DriveAnimatorParams))]
        [Tooltip("Forward velocity param. Empty = off.")]
        [AnimatorParameter(nameof(_Animator), AnimatorControllerParameterType.Float)]
        [SerializeField]
        private string _VerticalParam = "Vertical";

        [EnhancedFoldoutGroup("Animator")]
        [ShowIf(nameof(_DriveAnimatorParams))]
        [Tooltip("Moving bool param. Empty = off.")]
        [AnimatorParameter(nameof(_Animator), AnimatorControllerParameterType.Bool)]
        [SerializeField]
        private string _IsMovingParam = "IsMoving";

        [EnhancedFoldoutGroup("Animator")]
        [ShowIf(nameof(_DriveAnimatorParams))]
        [Tooltip("Playback speed param. Empty = off.")]
        [AnimatorParameter(nameof(_Animator), AnimatorControllerParameterType.Float)]
        [SerializeField]
        private string _AnimationSpeedParam = "Speed";

        [EnhancedFoldoutGroup("Animator")]
        [ShowIf(nameof(_DriveAnimatorParams))]
        [Tooltip("Turn param, -2..2. Empty = off.")]
        [AnimatorParameter(nameof(_Animator), AnimatorControllerParameterType.Float)]
        [SerializeField]
        private string _TurnParam = "Turn";

        [EnhancedFoldoutGroup("Animator")]
        [ShowIf(nameof(_DriveAnimatorParams))]
        [Tooltip("Crouch param, 0..1. Empty = off.")]
        [AnimatorParameter(nameof(_Animator), AnimatorControllerParameterType.Float)]
        [SerializeField]
        private string _CrouchParam;

        [EnhancedFoldoutGroup("Targets", "@ColorTools.Cyan", SpaceAfter = 4)]
        [SerializeField]
        [EnhancedInlineProperty]
        [Tooltip("Head/HMD target.")]
        private TargetRecord _HeadTarget;

        [EnhancedFoldoutGroup("Targets")]
        [SerializeField]
        [EnhancedInlineProperty]
        [Tooltip("Left hand target.")]
        private TargetRecord _LeftHandTarget;

        [EnhancedFoldoutGroup("Targets")]
        [SerializeField]
        [EnhancedInlineProperty]
        [Tooltip("Right hand target.")]
        private TargetRecord _RightHandTarget;

        [EnhancedFoldoutGroup("Bones", "@ColorTools.Orange", SpaceAfter = 4)]
        [Required]
        [SerializeField]
        private Transform _Hips;

        [EnhancedFoldoutGroup("Bones")]
        [SerializeField]
        [Tooltip("Spine bone, between hips and chest.")]
        private Transform _Spine;

        [EnhancedFoldoutGroup("Bones")]
        [SerializeField]
        private Transform _Chest;

        [EnhancedFoldoutGroup("Bones")]
        [SerializeField]
        [Tooltip("Neck bone. Takes part of the head turn.")]
        private Transform _Neck;

        [EnhancedFoldoutGroup("Bones")]
        [Required]
        [SerializeField]
        private Transform _Head;

        [EnhancedFoldoutGroup("Bones")]
        [EnhancedInlineProperty]
        [SerializeField]
        private ArmRecord _LeftArm;

        [EnhancedFoldoutGroup("Bones")]
        [EnhancedInlineProperty]
        [SerializeField]
        private ArmRecord _RightArm;

        [EnhancedFoldoutGroup("Bones")]
        [EnhancedInlineProperty]
        [SerializeField]
        private LegRecord _LeftLeg;

        [EnhancedFoldoutGroup("Bones")]
        [EnhancedInlineProperty]
        [SerializeField]
        private LegRecord _RightLeg;

        [EnhancedFoldoutGroup("Weights", "@ColorTools.Lime", SpaceAfter = 4)]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Master IK weight. Fades IK in/out.")]
        private float _Weight = 1f;

        [EnhancedFoldoutGroup("Weights")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _HeadPositionWeight = 1f;

        [EnhancedFoldoutGroup("Weights")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _HeadRotationWeight = 1f;

        [EnhancedFoldoutGroup("Weights")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _LeftHandPositionWeight = 1f;

        [EnhancedFoldoutGroup("Weights")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _LeftHandRotationWeight = 1f;

        [EnhancedFoldoutGroup("Weights")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _RightHandPositionWeight = 1f;

        [EnhancedFoldoutGroup("Weights")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _RightHandRotationWeight = 1f;

        [EnhancedFoldoutGroup("Root", "@ColorTools.Aqua", SpaceAfter = 4)]
        [SerializeField]
        [Tooltip("Root follows the head. Off = an external system moves the avatar.")]
        private bool _RootFollowsHead = true;

        [EnhancedFoldoutGroup("Root")]
        [SerializeField]
        [Range(0f, 180f)]
        [Tooltip("Max head-vs-root angle before the root turns. Standing.")]
        private float _MaxRootAngleStanding = 80f;

        [EnhancedFoldoutGroup("Root")]
        [SerializeField]
        [Range(0f, 180f)]
        [Tooltip("Max head-vs-root angle before the root turns. Moving.")]
        private float _MaxRootAngleMoving = 15f;

        [EnhancedFoldoutGroup("Root")]
        [ShowIf(nameof(_RootFollowsHead))]
        [SerializeField]
        [Tooltip("Max head-to-root distance before the root is dragged.")]
        private float _MaxRootOffset = 0.5f;

        [EnhancedFoldoutGroup("Root")]
        [ShowIf(nameof(_RootFollowsHead))]
        [SerializeField]
        [Tooltip("Root catch-up rate while walking. Higher = less leg drag.")]
        private float _RootCatchUpSpeedMoving = 30f;

        [EnhancedFoldoutGroup("Root")]
        [ShowIf(nameof(_RootFollowsHead))]
        [SerializeField]
        [Tooltip("Root catch-up rate while turning.")]
        private float _RootCatchUpSpeedTurning = 10f;

        [EnhancedFoldoutGroup("Body", "@ColorTools.Green", SpaceAfter = 4)]
        [SerializeField]
        [Tooltip("Use the avatar's standing height, not the player's.")]
        private bool _UseAvatarHeight;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the hips snap with the head turn.")]
        private float _BodyRotStiffness = 0.1f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Range(0f, 180f)]
        [Tooltip("Max head turn before the chest follows.")]
        private float _HeadMaxAngle = 65f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of the head turn the neck takes.")]
        private float _NeckBendWeight = 0.45f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the chest aims at the hands.")]
        private float _RotateChestByHands = 1f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much of the head offset the hips take. Spine covers the rest.")]
        private float _BodyPositionStiffness = 0.55f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the head turn bends the spine.")]
        private float _SpineBendWeight = 0.2f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        private float _SpineBendClamp = 60f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Tooltip("Spine arch per meter of crouch, in degrees.")]
        private float _CrouchBendAngle = 30f;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Tooltip("Vertical dead zone where the head keeps the animated height. 0 = pin exactly.")]
        private float _AnimatedHeadHeightRange;

        [EnhancedFoldoutGroup("Body")]
        [SerializeField]
        [Tooltip("Blend range back to target height past the dead zone.")]
        private float _AnimatedHeadHeightBlend = 0.3f;
        
        [EnhancedFoldoutGroup("Arms", "@ColorTools.Magenta", SpaceAfter = 4)]
        [SerializeField]
        [Tooltip("Default elbow hint direction, root space. Mirrored on x for left.")]
        private Vector3 _ElbowBaseDirection = new(0.35f, -1f, -0.25f);

        [EnhancedFoldoutGroup("Arms")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the hand's rotation tilts the elbow hint.")]
        private float _WristBendInfluence = 0.3f;

        [EnhancedFoldoutGroup("Arms")]
        [SerializeField]
        [FormerlySerializedAs("_WristPoleAxis")]
        [Tooltip("Hand axis that tilts the elbow hint. Mirrored on x for left.")]
        private Vector3 _WristHintAxis = new(0f, -1f, 0f);

        [EnhancedFoldoutGroup("Arms")]
        [SerializeField]
        [Range(0.5f, 1f)]
        [Tooltip("Max arm reach fraction. Stops elbow snap when extended.")]
        private float _MaxArmExtension = 0.98f;

        [EnhancedFoldoutGroup("Arms")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the shoulder turns to the hand. Needs shoulder bones.")]
        private float _ShoulderRotationWeight = 1f;

        [EnhancedFoldoutGroup("Arms")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Upper arm twist when the hand lifts.")]
        private float _ShoulderTwistWeight = 1f;

        [EnhancedFoldoutGroup("Arms")]
        [SerializeField]
        [Range(0f, 90f)]
        [Tooltip("Max shoulder swing toward the hand.")]
        private float _ShoulderMaxAngle = 30f;
        
        [EnhancedFoldoutGroup("Arms", "@ColorTools.Yellow", SpaceAfter = 4)]
        [SerializeField]
        [Tooltip("Drop hand IK to animation when hands rest low.")]
        private bool _AutoDropHandsToAnimation = true;

        [EnhancedFoldoutGroup("Arms")]
        [ShowIf(nameof(_AutoDropHandsToAnimation))]
        [SerializeField]
        [Tooltip("Hand speed below this counts as resting, m/s.")]
        private float _HandStillSpeedThreshold = 0.15f;

        [EnhancedFoldoutGroup("Arms")]
        [ShowIf(nameof(_AutoDropHandsToAnimation))]
        [SerializeField]
        [Tooltip("Rest time before dropping to animation.")]
        private float _HandStillTime = 0.5f;

        [EnhancedFoldoutGroup("Arms")]
        [ShowIf(nameof(_AutoDropHandsToAnimation))]
        [SerializeField]
        [Tooltip("Hands must be this far below the head to drop.")]
        private float _HandDropHeightBelowHead = 0.4f;

        [EnhancedFoldoutGroup("Arms")]
        [ShowIf(nameof(_AutoDropHandsToAnimation))]
        [SerializeField]
        private float _HandWeightFadeSpeed = 4f;
        
        [EnhancedFoldoutGroup("Legs", "@ColorTools.Blue", SpaceAfter = 4)]
        [SerializeField]
        [Range(0.5f, 1f)]
        [Tooltip("Max leg reach fraction. Limits hip pull from planted feet.")]
        private float _MaxLegStretch = 0.98f;
        
        [EnhancedFoldoutGroup("Legs")]
        [SerializeField]
        [Tooltip("Calculate: raycast feet to ground. ReadAnchors: read foot pose from the anchor. Disabled: keep the animated pose.")]
        private FootGroundingMode _GroundingMode = FootGroundingMode.Calculate;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf("@_GroundingMode == FootGroundingMode.Calculate || _RootFollowsGround")]
        [SerializeField]
        [Tooltip("Walkable layers. Exclude the avatar's own colliders.")]
        private LayerMask _GroundLayers = 1;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf("@_GroundingMode == FootGroundingMode.Calculate || _RootFollowsGround")]
        [SerializeField]
        private float _MaxStepHeight = 0.4f;

        [EnhancedFoldoutGroup("Legs")]
        [HideIf(nameof(_GroundingMode), FootGroundingMode.Disabled)]
        [SerializeField]
        [Tooltip("Raises/lowers grounded feet.")]
        private float _FootHeightOffset;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf(nameof(_GroundingMode), FootGroundingMode.Calculate)]
        [SerializeField]
        [Tooltip("How fast a foot eases down on first contact.")]
        private float _FootSpeed = 8f;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf(nameof(_GroundingMode), FootGroundingMode.Calculate)]
        [SerializeField]
        [Range(0f, 90f)]
        private float _MaxFootRotationAngle = 45f;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf(nameof(_GroundingMode), FootGroundingMode.Calculate)]
        [SerializeField]
        private float _FootRotationSpeed = 7f;

        [EnhancedFoldoutGroup("Legs")]
        [HideIf(nameof(_GroundingMode), FootGroundingMode.Disabled)]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the pelvis drops for feet below the root.")]
        private float _PelvisGroundingWeight = 1f;

        [EnhancedFoldoutGroup("Legs")]
        [SerializeField]
        [Tooltip("Root Y follows the ground under the head. Off = external system sets height.")]
        private bool _RootFollowsGround = true;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf(nameof(_RootFollowsGround))]
        [SerializeField]
        [Tooltip("Ground probe start, distance below the head.")]
        private float _RootProbeHeadOffset = 1f;

        [EnhancedFoldoutGroup("Legs")]
        [ShowIf(nameof(_RootFollowsGround))]
        [SerializeField]
        [Tooltip("Smoothing for the root height follow.")]
        private float _GroundSmoothTime = 0.1f;

        [EnhancedFoldoutGroup("Locomotion", "@ColorTools.Pink", SpaceAfter = 4)]
        [SerializeField]
        [Tooltip("Speed to count as moving, m/s.")]
        private float _MoveThreshold = 0.3f;

        [EnhancedFoldoutGroup("Locomotion")]
        [SerializeField]
        [Tooltip("Blend-tree velocity smoothing. Lower = more responsive.")]
        private float _VelocitySmoothTime = 0.1f;

        [EnhancedFoldoutGroup("Locomotion")]
        [SerializeField]
        [Range(0f, 90f)]
        [Tooltip("Head-vs-root angle before the turn-in-place animation starts. Higher = less sensitive.")]
        private float _TurnStartAngle = 30f;

        [EnhancedFoldoutGroup("Locomotion")]
        [SerializeField]
        [Range(0.5f, 10f)]
        [Tooltip("How fast the Turn param eases toward its target. Lower = slower, gentler turn-in-place.")]
        private float _TurnResponseSpeed = 5f;

        [EnhancedFoldoutGroup("Locomotion")]
        [MinMaxSlider(0f, 5f, true)]
        [SerializeField]
        [Tooltip("Min/max playback speed.")]
        private Vector2 _AnimationSpeedRange = new(0.2f, 3f);

        [EnhancedFoldoutGroup("Locomotion")]
        [SerializeField]
        [Tooltip("Head drop mapping to crouch = 1, in meters.")]
        private float _CrouchRange = 0.5f;

        [EnhancedFoldoutGroup("Locomotion")]
        [SerializeField]
        [Tooltip("Head jump above this in one frame = teleport, resets velocity.")]
        private float _TeleportDistanceThreshold = 1f;

        public float Weight { get => _Weight; set => _Weight = Mathf.Clamp01(value); }
        public float LeftHandPositionWeight { get => _LeftHandPositionWeight; set => _LeftHandPositionWeight = Mathf.Clamp01(value); }
        public float LeftHandRotationWeight { get => _LeftHandRotationWeight; set => _LeftHandRotationWeight = Mathf.Clamp01(value); }
        public float RightHandPositionWeight { get => _RightHandPositionWeight; set => _RightHandPositionWeight = Mathf.Clamp01(value); }
        public float RightHandRotationWeight { get => _RightHandRotationWeight; set => _RightHandRotationWeight = Mathf.Clamp01(value); }
        public bool AutoDropHandsToAnimation { get => _AutoDropHandsToAnimation; set => _AutoDropHandsToAnimation = value; }
        public TargetRecord HeadTarget => _HeadTarget;
        public TargetRecord LeftHandTarget => _LeftHandTarget;
        public TargetRecord RightHandTarget => _RightHandTarget;
        public bool IsMoving => _isMoving;

        private Transform[] _spineChain;
        private float[] _spineWeights;

        private Vector3 _leftHandLastPos;
        private Vector3 _rightHandLastPos;
        private float _leftHandStillTime;
        private float _rightHandStillTime;
        private float _leftHandAutoWeight = 1f;
        private float _rightHandAutoWeight = 1f;
        private bool _handStateInitialized;

        private int _horizontalParamHash;
        private int _verticalParamHash;
        private int _isMovingParamHash;
        private int _animationSpeedParamHash;
        private int _turnParamHash;
        private int _crouchParamHash;

        private float _currentCrouch;
        private float _headHeightOffset;
        private bool _heightOffsetCalibrated;

        // bind pose, set in OnEnable
        private Quaternion _anchorRelativeToHead = Quaternion.identity;
        private float _standingHeadHeight;

        private Vector3 _lastHeadTargetPos;
        private Vector3 _lastSolveEndRootPos;
        private Vector3 _lastRootCorrection;
        private Vector3 _velocityLocal;
        private Vector3 _velocityLocalDamp;
        private float _animationSpeed = 1f;
        private float _animationSpeedDamp;
        private float _currentVelocitySmoothTime = 0.05f;
        private float _clipUnitSpeed = 1f;
        private float _stopMoveTimer = 1f;
        private float _catchUpSpeed;
        private float _turn;
        private float _currentMaxRootAngle;
        private float _groundDampVelocity;
        private float _pelvisGroundOffset;
        private bool _isMoving;
        private bool _turning;
        private bool _hasLastState;
        private readonly RaycastHit[] _rayHits = new RaycastHit[1];

        private void OnEnable()
        {
            RefreshChains();
            _heightOffsetCalibrated = false;
            _hasLastState = false;
            _handStateInitialized = false;
            _leftHandAutoWeight = 1f;
            _rightHandAutoWeight = 1f;
            _leftHandStillTime = 0f;
            _rightHandStillTime = 0f;
            _velocityLocal = Vector3.zero;
            _velocityLocalDamp = Vector3.zero;
            _currentVelocitySmoothTime = 0.05f;
            _lastRootCorrection = Vector3.zero;
            _animationSpeed = 1f;
            _clipUnitSpeed = 1f;
            _stopMoveTimer = 1f;
            _catchUpSpeed = 0f;
            _isMoving = false;
            _turning = false;
            _turn = 0f;
            _currentMaxRootAngle = _MaxRootAngleStanding;
            
            CalibrateRig();
        }

        private void LateUpdate()
        {
            Solve();
        }

        public void Solve()
        {
            var deltaTime = Time.deltaTime;
            if (_Weight <= 0f || deltaTime <= 0f || _Head == null || _Hips == null || !_HeadTarget.IsValid)
                return;

            var headTargetPos = _HeadTarget.GetPosition();
            var headTargetRot = _HeadTarget.GetRotation();
            if (_UseAvatarHeight)
            {
                if (!_heightOffsetCalibrated)
                    TryCalibrateHeightOffset(headTargetPos);
                if (_heightOffsetCalibrated)
                    headTargetPos.y -= _headHeightOffset;
            }

            // body heading from head
            var anchorRot = headTargetRot * _anchorRelativeToHead;
            var animatedRootRotation = transform.rotation;

            _currentCrouch = Mathf.Max(0f, _Head.position.y - headTargetPos.y);

            // pelvis rot relative to head
            var pelvisRelativeRotation = Quaternion.Inverse(_Head.rotation) * _Hips.rotation;

            // record feet before root moves, keeps them planted
            if (_LeftLeg.IsValid)
                _LeftLeg.Record();
            if (_RightLeg.IsValid)
                _RightLeg.Record();

            UpdateLocomotion(headTargetPos, anchorRot, animatedRootRotation, deltaTime);
            SolveRootRotation(anchorRot, headTargetPos);
            ResolveRootHeight(headTargetPos);

            var leftGroundOffset = 0f;
            var rightGroundOffset = 0f;
            switch (_GroundingMode)
            {
                case FootGroundingMode.Calculate:
                    leftGroundOffset = GroundLeg(_LeftLeg, deltaTime);
                    rightGroundOffset = GroundLeg(_RightLeg, deltaTime);
                    _LeftLeg.WriteGroundingAnchor();
                    _RightLeg.WriteGroundingAnchor();
                    break;
                case FootGroundingMode.ReadAnchors:
                    leftGroundOffset = _LeftLeg.ReadGroundingAnchors(transform.position.y, _FootHeightOffset);
                    rightGroundOffset = _RightLeg.ReadGroundingAnchors(transform.position.y, _FootHeightOffset);
                    break;
                case FootGroundingMode.Disabled:
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            // resting low hands fade to anim
            UpdateHandAutoWeights(headTargetPos, anchorRot, deltaTime);
            var leftHandPositionWeight = _LeftHandPositionWeight * _leftHandAutoWeight;
            var rightHandPositionWeight = _RightHandPositionWeight * _rightHandAutoWeight;

            // hips snap a bit with head
            if (_BodyRotStiffness > 0f)
            {
                var bodyDelta = ClampRotation(headTargetRot * pelvisRelativeRotation * Quaternion.Inverse(_Hips.rotation), 90f);
                _Hips.rotation = Quaternion.Slerp(Quaternion.identity, bodyDelta, _BodyRotStiffness * _Weight) * _Hips.rotation;
            }

            SolveSpine(headTargetRot, GetChestRotationByHands(headTargetPos, anchorRot, leftHandPositionWeight, rightHandPositionWeight));
            // hips take part, then pin head exactly
            SolveBodyPosition(headTargetPos, _BodyPositionStiffness, false);
            SolveHeadRotation(headTargetRot);
            SolveBodyPosition(headTargetPos, 1f, false);
            GroundPelvis(Mathf.Min(leftGroundOffset, rightGroundOffset), deltaTime);

            SolveLeg(_LeftLeg);
            SolveLeg(_RightLeg);

            SolveArm(_LeftArm, _LeftHandTarget, true, leftHandPositionWeight, _LeftHandRotationWeight * _leftHandAutoWeight);
            SolveArm(_RightArm, _RightHandTarget, false, rightHandPositionWeight, _RightHandRotationWeight * _rightHandAutoWeight);

            _lastSolveEndRootPos = transform.position;
        }

        private void SolveHeadRotation(Quaternion headTargetRot)
        {
            var w = _HeadRotationWeight * _Weight;
            if (w <= 0f)
                return;

            // head turn from anim pose, capped
            var delta = ClampRotation(headTargetRot * Quaternion.Inverse(_Head.rotation), _HeadMaxAngle);

            // neck takes part of the turn
            if (_Neck != null)
                _Neck.rotation = Quaternion.Slerp(Quaternion.identity, delta, _NeckBendWeight * w) * _Neck.rotation;

            // head finishes on target
            _Head.rotation = Quaternion.Slerp(_Head.rotation, headTargetRot, w);
        }

        private void UpdateHandAutoWeights(Vector3 headTargetPos, Quaternion anchorRot, float deltaTime)
        {
            if (!_AutoDropHandsToAnimation)
            {
                _leftHandAutoWeight = 1f;
                _rightHandAutoWeight = 1f;
                return;
            }

            // heading only, ignore look up/down
            var headingRot = GetHeadingRotation(anchorRot);

            if (!_handStateInitialized)
            {
                if (_LeftHandTarget.IsValid)
                    _leftHandLastPos = GetHandRelativePosition(_LeftHandTarget, headTargetPos, headingRot);
                if (_RightHandTarget.IsValid)
                    _rightHandLastPos = GetHandRelativePosition(_RightHandTarget, headTargetPos, headingRot);
                _handStateInitialized = true;
                return;
            }

            UpdateHandAutoWeight(_LeftHandTarget, headTargetPos, headingRot, deltaTime, ref _leftHandLastPos, ref _leftHandStillTime, ref _leftHandAutoWeight);
            UpdateHandAutoWeight(_RightHandTarget, headTargetPos, headingRot, deltaTime, ref _rightHandLastPos, ref _rightHandStillTime, ref _rightHandAutoWeight);
        }
        
        private static Quaternion GetHeadingRotation(Quaternion anchorRot)
        {
            var forward = anchorRot * Vector3.forward;
            forward.y = 0f;

            // straight up/down, use right axis
            if (forward.sqrMagnitude < 1e-4f)
            {
                forward = Vector3.Cross(anchorRot * Vector3.right, Vector3.up);
                if (forward.sqrMagnitude < 1e-4f)
                    return Quaternion.identity;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
        
        private static Vector3 GetHandRelativePosition(TargetRecord target, Vector3 headTargetPos, Quaternion headingRot)
        {
            return Quaternion.Inverse(headingRot) * (target.GetPosition() - headTargetPos);
        }

        private void UpdateHandAutoWeight(TargetRecord target, Vector3 headTargetPos, Quaternion headingRot, float deltaTime, ref Vector3 lastPos, ref float stillTimer, ref float autoWeight)
        {
            if (!target.IsValid)
                return;

            var relativePosition = GetHandRelativePosition(target, headTargetPos, headingRot);
            var delta = relativePosition - lastPos;
            lastPos = relativePosition;

            var speed = delta.magnitude / deltaTime;
            stillTimer = speed < _HandStillSpeedThreshold ? stillTimer + deltaTime : 0f;

            var isBelowShoulders = target.GetPosition().y < headTargetPos.y - _HandDropHeightBelowHead;
            var weightTarget = stillTimer >= _HandStillTime && isBelowShoulders ? 0f : 1f;
            autoWeight = Mathf.MoveTowards(autoWeight, weightTarget, _HandWeightFadeSpeed * deltaTime);
        }

        private float SignedHeadingAngle(Quaternion anchorRot)
        {
            var headingForward = GetHeadingRotation(anchorRot) * Vector3.forward;
            var local = Quaternion.Inverse(transform.rotation) * headingForward;
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        private void UpdateLocomotion(Vector3 headTargetPos, Quaternion anchorRot, Quaternion animatedRootRotation, float deltaTime)
        {
            const float stopDelay = 0.06f; // debounce IsMoving after stop

            if (!_hasLastState)
            {
                _lastHeadTargetPos = headTargetPos;
                _hasLastState = true;
                return;
            }

            // head motion this frame; big jump = teleport
            var headStep = headTargetPos - _lastHeadTargetPos;
            _lastHeadTargetPos = headTargetPos;
            headStep.y = 0f;
            if (headStep.magnitude > _TeleportDistanceThreshold)
            {
                _velocityLocal = Vector3.zero;
                _velocityLocalDamp = Vector3.zero;
                _lastRootCorrection = Vector3.zero;
                _lastSolveEndRootPos = transform.position;
                return;
            }

            var headVelocityWorld = headStep / deltaTime;

            // head-to-root offset, minus last catch-up so it's not double counted
            var offset = headTargetPos - transform.position;
            offset -= _lastRootCorrection;
            offset.y = 0f;

            // heading vs root; before smoothing so turn-in-place damps its own step
            var headingAngle = SignedHeadingAngle(anchorRot);
            var absHeading = Mathf.Abs(headingAngle);

            // hysteresis, no flicker at the threshold
            var turnBar = _turning ? _TurnStartAngle * 0.7f : _TurnStartAngle;
            _turning = absHeading > turnBar;
            var isTurning = _turning;

            // ramp Turn from deadzone (0 at start angle, full at 90) so crossing doesn't snap
            var over = Mathf.Max(0f, absHeading - _TurnStartAngle);
            var turnTarget = Mathf.Sign(headingAngle) * Mathf.Clamp01(over / Mathf.Max(90f - _TurnStartAngle, 1f));
            _turn = Mathf.Lerp(_turn, turnTarget, deltaTime * _TurnResponseSpeed);

            // command = head motion + offset, body space
            var commandWorld = headVelocityWorld + offset;
            var velocityTarget = Quaternion.Inverse(animatedRootRotation) * commandWorld * _Weight;

            // heavier smoothing turning in place, so a head turn doesn't spike a step
            var smoothTimeTarget = isTurning && !_isMoving ? 0.2f : _VelocitySmoothTime;
            _currentVelocitySmoothTime = Mathf.Lerp(_currentVelocitySmoothTime, smoothTimeTarget, deltaTime * 20f);
            _velocityLocal = Vector3.SmoothDamp(_velocityLocal, velocityTarget, ref _velocityLocalDamp, _currentVelocitySmoothTime, Mathf.Infinity, deltaTime);

            // separate start/stop bars + hold, no flicker
            var speed = _velocityLocal.magnitude;
            var bar = _isMoving ? _MoveThreshold * 0.7f : _MoveThreshold;
            var movingNow = speed > bar;
            _stopMoveTimer = movingNow ? 0f : _stopMoveTimer + deltaTime;
            _isMoving = _stopMoveTimer < stopDelay;

            // wider deadzone standing, tighter moving
            var maxAngleTarget = _isMoving ? _MaxRootAngleMoving : _MaxRootAngleStanding;
            _currentMaxRootAngle = Mathf.Lerp(_currentMaxRootAngle, maxAngleTarget, 1f - Mathf.Exp(-deltaTime * 6f));

            // estimate clip 1x speed from real travel, playback = command / that. avg, stops sliding
            var rootTravel = transform.position - _lastSolveEndRootPos;
            rootTravel.y = 0f;
            var measuredUnitSpeed = rootTravel.magnitude / deltaTime / Mathf.Max(_animationSpeed, 0.01f);
            if (movingNow && measuredUnitSpeed > 0.01f)
                _clipUnitSpeed = Mathf.Lerp(_clipUnitSpeed, measuredUnitSpeed, 1f - Mathf.Exp(-deltaTime * 4f));

            var speedTarget = movingNow ? speed / Mathf.Max(_clipUnitSpeed, 0.01f) : _AnimationSpeedRange.x;
            speedTarget = Mathf.Clamp(speedTarget, _AnimationSpeedRange.x, _AnimationSpeedRange.y);
            _animationSpeed = Mathf.SmoothDamp(_animationSpeed, speedTarget, ref _animationSpeedDamp, 0.05f, Mathf.Infinity, deltaTime);

            if (_DriveAnimatorParams && _Animator != null)
            {
                if (_horizontalParamHash != 0)
                    _Animator.SetFloat(_horizontalParamHash, _velocityLocal.x);
                if (_verticalParamHash != 0)
                    _Animator.SetFloat(_verticalParamHash, _velocityLocal.z);
                if (_isMovingParamHash != 0)
                    _Animator.SetBool(_isMovingParamHash, _isMoving);
                if (_animationSpeedParamHash != 0)
                    _Animator.SetFloat(_animationSpeedParamHash, Mathf.Lerp(1f, _animationSpeed, _Weight));
                if (_turnParamHash != 0)
                    _Animator.SetFloat(_turnParamHash, _turn * 2f);
                if (_crouchParamHash != 0)
                    _Animator.SetFloat(_crouchParamHash, Mathf.Clamp01(_currentCrouch / Mathf.Max(_CrouchRange, 0.001f)));
            }

            if (!_RootFollowsHead)
            {
                _lastRootCorrection = Vector3.zero;
                return;
            }

            // chase head only moving/turning, torso stays over feet. 0.2 floor closes settled offset
            var chaseBase = _isMoving ? _RootCatchUpSpeedMoving : isTurning ? _RootCatchUpSpeedTurning : 0f;
            var chaseRate = chaseBase * Mathf.Max(headVelocityWorld.magnitude, 0.2f);
            _catchUpSpeed = Mathf.Lerp(_catchUpSpeed, chaseRate, 1f - Mathf.Exp(-deltaTime * 18f));

            var before = transform.position;
            var target = new Vector3(headTargetPos.x, transform.position.y, headTargetPos.z);
            if (_catchUpSpeed > 0f)
                transform.position = Vector3.Lerp(transform.position, target, (1f - Mathf.Exp(-_catchUpSpeed * deltaTime)) * _Weight);

            // clamp to max offset
            var slack = target - transform.position;
            slack.y = 0f;
            var slackMagnitude = slack.magnitude;
            if (slackMagnitude > _MaxRootOffset)
                transform.position += slack - slack / slackMagnitude * _MaxRootOffset;

            _lastRootCorrection = transform.position - before;
        }

        private void SolveRootRotation(Quaternion anchorRot, Vector3 headTargetPos)
        {
            var headingAngle = SignedHeadingAngle(anchorRot);
            var overshoot = Mathf.Max(0f, Mathf.Abs(headingAngle) - _currentMaxRootAngle) * Mathf.Sign(headingAngle);
            if (Mathf.Approximately(overshoot, 0f))
                return;

            var yaw = Quaternion.AngleAxis(overshoot, Vector3.up);

            // pivot under head, headset stays put while root yaws
            var pivot = new Vector3(headTargetPos.x, transform.position.y, headTargetPos.z);
            transform.position = pivot + yaw * (transform.position - pivot);
            transform.rotation = yaw * transform.rotation;
        }

        private void ResolveRootHeight(Vector3 headTargetPos)
        {
            if (!_RootFollowsGround)
                return;

            // probe down from waist; ground in range else drop with head
            var probeOrigin = headTargetPos - Vector3.up * _RootProbeHeadOffset;
            var range = _standingHeadHeight - _RootProbeHeadOffset + _MaxStepHeight;
            var ray = new Ray(probeOrigin, Vector3.down);
            var targetY = Physics.RaycastNonAlloc(ray, _rayHits, range, _GroundLayers.value) > 0
                ? _rayHits[0].point.y
                : headTargetPos.y - _standingHeadHeight;

            var position = transform.position;
            position.y = _GroundSmoothTime > 0f
                ? Mathf.SmoothDamp(position.y, targetY, ref _groundDampVelocity, _GroundSmoothTime)
                : targetY;
            transform.position = position;
        }

        private void SolveSpine(Quaternion headTargetRot, Quaternion chestRotationByHands)
        {
            if (_spineChain == null || _spineChain.Length == 0)
                return;

            var crouchArch = Quaternion.AngleAxis(_currentCrouch * _CrouchBendAngle * _Weight, transform.right);
            var chestAdjust = ClampRotation(chestRotationByHands, _SpineBendClamp);

            for (var i = 0; i < _spineChain.Length; i++)
            {
                var bone = _spineChain[i];
                if (bone == null)
                    continue;

                var remaining = ClampRotation(headTargetRot * Quaternion.Inverse(_Head.rotation), _SpineBendClamp);
                var bend = Quaternion.Slerp(Quaternion.identity, remaining, _spineWeights[i] * _SpineBendWeight * _Weight);
                var arch = Quaternion.Slerp(Quaternion.identity, crouchArch, _spineWeights[i]);

                // chest aim to hands
                bend = Quaternion.Slerp(Quaternion.identity, chestAdjust, _spineWeights[i] * _SpineBendWeight * _Weight) * bend;

                bone.rotation = bend * arch * bone.rotation;
            }
        }

        private const float CHEST_HANDS_MAX_ANGLE = 30f; // clamp per axis
        private const float CHEST_HANDS_FORWARD_FLOOR = 0.25f; // min forward reach, stable aim

        private Quaternion GetChestRotationByHands(Vector3 headTargetPos, Quaternion anchorRot, float leftHandWeight, float rightHandWeight)
        {
            if (_RotateChestByHands <= 0f || !_LeftHandTarget.IsValid || !_RightHandTarget.IsValid)
                return Quaternion.identity;

            var totalWeight = leftHandWeight + rightHandWeight;
            if (totalWeight <= 0f)
                return Quaternion.identity;

            var bodySize = Vector3.Distance(_Hips.position, _Head.position);
            if (bodySize < 0.01f)
                return Quaternion.identity;

            // hands rel to head, body space, weighted mid
            var toBodySpace = Quaternion.Inverse(anchorRot);
            var leftLocal = toBodySpace * (_LeftHandTarget.GetPosition() - headTargetPos);
            var rightLocal = toBodySpace * (_RightHandTarget.GetPosition() - headTargetPos);
            var midpoint = (leftLocal * leftHandWeight + rightLocal * rightHandWeight) / totalWeight / bodySize;

            // min forward reach, close hands don't spin aim
            var forward = Mathf.Max(midpoint.z, CHEST_HANDS_FORWARD_FLOOR);

            // yaw to hands, small pitch from height
            var yawAngle = Mathf.Atan2(midpoint.x, forward) * Mathf.Rad2Deg * _RotateChestByHands;
            var tiltAngle = Mathf.Atan2(midpoint.y, forward) * Mathf.Rad2Deg * _RotateChestByHands * 0.5f;
            yawAngle = Mathf.Clamp(yawAngle, -CHEST_HANDS_MAX_ANGLE, CHEST_HANDS_MAX_ANGLE);
            tiltAngle = Mathf.Clamp(tiltAngle, -CHEST_HANDS_MAX_ANGLE, CHEST_HANDS_MAX_ANGLE);

            var yaw = Quaternion.AngleAxis(yawAngle, transform.up);
            var tilt = Quaternion.AngleAxis(-tiltAngle, transform.right);
            return tilt * yaw;
        }

        private void SolveBodyPosition(Vector3 headTargetPos, float stiffness, bool limitByLegs)
        {
            var w = _HeadPositionWeight * _Weight * stiffness;
            if (w <= 0f)
                return;

            var correction = headTargetPos - _Head.position;

            // deadzone keeps idle head bob
            if (_AnimatedHeadHeightRange > 0f)
            {
                var excess = Mathf.Max(0f, Mathf.Abs(correction.y) - _AnimatedHeadHeightRange);
                correction.y *= _AnimatedHeadHeightBlend > 0f
                    ? Mathf.SmoothStep(0f, 1f, excess / _AnimatedHeadHeightBlend)
                    : (excess > 0f ? 1f : 0f);
            }

            correction *= w;

            // keep hips within leg reach
            if (limitByLegs)
            {
                for (var i = 0; i < 2; i++)
                {
                    LimitCorrectionByLeg(_LeftLeg, ref correction);
                    LimitCorrectionByLeg(_RightLeg, ref correction);
                }
            }

            _Hips.position += correction;
        }

        private void LimitCorrectionByLeg(LegRecord leg, ref Vector3 correction)
        {
            if (!leg.IsValid)
                return;

            var wantedThigh = leg.Thigh.position + correction;
            var toWanted = wantedThigh - leg.FootPosition;
            var limitedThigh = leg.FootPosition + Vector3.ClampMagnitude(toWanted, leg.MaxLength * _MaxLegStretch);
            correction += limitedThigh - wantedThigh;
        }

        private void SolveLeg(LegRecord leg)
        {
            if (!leg.IsValid)
                return;

            // knee to hint, else forward
            var kneeHint = leg.Hint != null
                ? leg.Hint.position - leg.Thigh.position
                : transform.forward;
            leg.KneeHint = kneeHint;

            SolveTwoBone(leg.Thigh, leg.Calf, leg.Foot, leg.FootPosition, kneeHint, 1f, transform.forward);
            leg.Foot.rotation = leg.FootRotation;
        }

        private float GroundLeg(LegRecord leg, float deltaTime)
        {
            if (!leg.IsValid || _MaxStepHeight <= 0f)
                return 0f;

            var footPos = leg.FootPosition;
            var rootY = transform.position.y;

            var ray = new Ray(footPos + Vector3.up * _MaxStepHeight, Vector3.down);
            if (Physics.RaycastNonAlloc(ray, _rayHits, _MaxStepHeight * 2f, _GroundLayers.value) == 0)
            {
                // no ground, keep anim pose
                leg.ClearGrounding();
                return 0f;
            }

            var groundY = _rayHits[0].point.y;
            var groundNormal = _rayHits[0].normal;
            var offsetTarget = groundY - rootY;

            // lifted feet pull down less
            var animatedFootHeight = footPos.y - rootY;
            var maxDownOffset = Mathf.Clamp(_MaxStepHeight - animatedFootHeight, 0f, _MaxStepHeight);
            offsetTarget = Mathf.Clamp(offsetTarget, -maxDownOffset, _MaxStepHeight);

            leg.ApplyGrounding(offsetTarget, groundY, groundNormal, deltaTime, _FootSpeed, _FootRotationSpeed, _MaxFootRotationAngle, _FootHeightOffset);
            return leg.GroundOffset;
        }

        private void GroundPelvis(float lowestOffset, float deltaTime)
        {
            // drop pelvis so legs reach low feet
            var target = Mathf.Min(0f, lowestOffset) * _PelvisGroundingWeight;
            _pelvisGroundOffset = Mathf.Lerp(_pelvisGroundOffset, target, deltaTime * 5f);
            if (Mathf.Abs(_pelvisGroundOffset) > 0.0001f)
                _Hips.position += Vector3.up * _pelvisGroundOffset;
        }

        private void SolveArm(ArmRecord arm, TargetRecord target, bool isLeft, float positionWeight, float rotationWeight)
        {
            if (!arm.IsValid || !target.IsValid)
                return;

            positionWeight *= _Weight;
            rotationWeight *= _Weight;
            if (positionWeight <= 0f && rotationWeight <= 0f)
                return;

            var targetRot = target.GetRotation();

            if (positionWeight > 0f)
            {
                var targetPos = Vector3.Lerp(arm.Hand.position, target.GetPosition(), positionWeight);

                // clavicle first, upper body follows
                var shoulderLift = 0f;
                if (arm.Shoulder != null && _ShoulderRotationWeight > 0f)
                    shoulderLift = SolveShoulder(arm, targetPos, positionWeight);

                var hintDirection = GetElbowHint(arm, targetRot, isLeft);
                SolveTwoBone(arm.UpperArm, arm.Forearm, arm.Hand, targetPos, hintDirection, _MaxArmExtension, transform.forward);

                // twist upper arm as hand rises
                var twist = Mathf.Clamp(shoulderLift * positionWeight * _ShoulderRotationWeight * _ShoulderTwistWeight, 0f, 180f);
                if (twist > 0.01f && arm.Shoulder != null)
                {
                    ApplyTwist(arm.Shoulder, arm.UpperArm, isLeft ? twist : -twist);
                    ApplyTwist(arm.UpperArm, arm.Forearm, isLeft ? twist : -twist);
                }
            }

            if (rotationWeight > 0f)
                arm.Hand.rotation = Quaternion.Slerp(arm.Hand.rotation, targetRot, rotationWeight);
        }

        private float SolveShoulder(ArmRecord arm, Vector3 targetPos, float positionWeight)
        {
            var shoulder = arm.Shoulder;

            var currentDir = arm.UpperArm.position - shoulder.position;
            var targetDir = targetPos - shoulder.position;
            if (currentDir.sqrMagnitude < 1e-6f || targetDir.sqrMagnitude < 1e-6f)
                return 0f;

            // swing clavicle to hand, capped
            var toTarget = ClampRotation(Quaternion.FromToRotation(currentDir, targetDir), _ShoulderMaxAngle);
            var w = _ShoulderRotationWeight * positionWeight;
            shoulder.rotation = Quaternion.Slerp(Quaternion.identity, toTarget, w) * shoulder.rotation;

            // hand elevation, drives the twist
            var elevation = Vector3.Dot(targetDir.normalized, transform.up);
            return Mathf.Asin(Mathf.Clamp(elevation, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static void ApplyTwist(Transform bone, Transform child, float angle)
        {
            // twist around axis to child, keep child world rot
            var childRotation = child.rotation;
            bone.rotation = Quaternion.AngleAxis(angle, child.position - bone.position) * bone.rotation;
            child.rotation = childRotation;
        }

        private Vector3 GetElbowHint(ArmRecord arm, Quaternion handTargetRot, bool isLeft)
        {
            var baseDirection = _ElbowBaseDirection;
            var wristAxis = _WristHintAxis;
            if (isLeft)
            {
                baseDirection.x = -baseDirection.x;
                wristAxis.x = -wristAxis.x;
            }

            var hint = (transform.rotation * baseDirection).normalized;

            if (_WristBendInfluence > 0f && wristAxis != Vector3.zero)
                hint = Vector3.Slerp(hint, (handTargetRot * wristAxis).normalized, _WristBendInfluence);

            // hint from limb root (upper arm), like the leg
            if (arm.Hint != null && arm.HintWeight > 0f)
                hint = Vector3.Slerp(hint, (arm.Hint.position - arm.UpperArm.position).normalized, arm.HintWeight);

            return hint;
        }

        private static void SolveTwoBone(Transform upper, Transform lower, Transform end, Vector3 targetPos, Vector3 hintDirection, float maxExtension, Vector3 fallbackBendDirection)
        {
            // two-bone IK as delta rots, keeps anim roll
            var upperPos = upper.position;
            var upperLength = Vector3.Distance(upperPos, lower.position);
            var lowerLength = Vector3.Distance(lower.position, end.position);
            var maxLength = (upperLength + lowerLength) * maxExtension;

            var toTarget = targetPos - upperPos;
            var targetDistance = toTarget.magnitude;
            if (targetDistance < 0.001f)
                return;

            var direction = toTarget / targetDistance;
            targetDistance = Mathf.Clamp(targetDistance, 0.01f, maxLength);
            targetPos = upperPos + direction * targetDistance;

            var bendAxis = Vector3.Cross(direction, hintDirection);
            if (bendAxis.sqrMagnitude < 1e-6f)
                bendAxis = Vector3.Cross(direction, fallbackBendDirection);
            bendAxis.Normalize();

            // law of cosines, upper bone angle
            var cosAngle = (targetDistance * targetDistance + upperLength * upperLength - lowerLength * lowerLength)
                           / (2f * targetDistance * upperLength);
            var upperAngle = Mathf.Acos(Mathf.Clamp(cosAngle, -1f, 1f)) * Mathf.Rad2Deg;
            var midPos = upperPos + Quaternion.AngleAxis(upperAngle, bendAxis) * direction * upperLength;

            upper.rotation = Quaternion.FromToRotation(lower.position - upperPos, midPos - upperPos) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, targetPos - lower.position) * lower.rotation;
        }

        public void CalibrateRig()
        {
            // bind pose, before animator runs
            if (_Head == null || _Hips == null)
                return;

            _anchorRelativeToHead = Quaternion.Inverse(_Head.rotation) * transform.rotation;
            _standingHeadHeight = _Head.position.y - transform.position.y;
        }

        public void RequestRecalibration() => _heightOffsetCalibrated = false;

        private void TryCalibrateHeightOffset(Vector3 headTargetPos)
        {
            // wait for head target placed (not origin)
            if (headTargetPos.y - transform.position.y < _standingHeadHeight * 0.5f)
                return;

            _headHeightOffset = headTargetPos.y - (transform.position.y + _standingHeadHeight);
            _heightOffsetCalibrated = true;
        }

        public void RefreshChains()
        {
            // neck solved in SolveHeadRotation
            var chain = new List<Transform>(2);
            if (_Spine != null) chain.Add(_Spine);
            if (_Chest != null) chain.Add(_Chest);
            _spineChain = chain.ToArray();

            // progressive weights, upper bends more, sum 1
            _spineWeights = new float[_spineChain.Length];
            var sum = 0f;
            for (var i = 0; i < _spineWeights.Length; i++)
                sum += i + 1;
            for (var i = 0; i < _spineWeights.Length; i++)
                _spineWeights[i] = (i + 1) / Mathf.Max(sum, 1f);

            _horizontalParamHash = string.IsNullOrEmpty(_HorizontalParam) ? 0 : Animator.StringToHash(_HorizontalParam);
            _verticalParamHash = string.IsNullOrEmpty(_VerticalParam) ? 0 : Animator.StringToHash(_VerticalParam);
            _isMovingParamHash = string.IsNullOrEmpty(_IsMovingParam) ? 0 : Animator.StringToHash(_IsMovingParam);
            _animationSpeedParamHash = string.IsNullOrEmpty(_AnimationSpeedParam) ? 0 : Animator.StringToHash(_AnimationSpeedParam);
            _turnParamHash = string.IsNullOrEmpty(_TurnParam) ? 0 : Animator.StringToHash(_TurnParam);
            _crouchParamHash = string.IsNullOrEmpty(_CrouchParam) ? 0 : Animator.StringToHash(_CrouchParam);
        }

        private static Quaternion ClampRotation(Quaternion rotation, float maxAngle)
        {
            rotation.ToAngleAxis(out var angle, out var axis);
            if (float.IsInfinity(axis.x) || float.IsNaN(axis.x))
                return Quaternion.identity;

            if (angle > 180f)
            {
                angle = 360f - angle;
                axis = -axis;
            }

            return angle <= maxAngle ? rotation : Quaternion.AngleAxis(maxAngle, axis);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawTargetGizmo(_HeadTarget, Color.cyan, _Head);
            DrawTargetGizmo(_LeftHandTarget, Color.green, _LeftArm.Hand);
            DrawTargetGizmo(_RightHandTarget, Color.yellow, _RightArm.Hand);

            if (_LeftArm.IsValid)
                DrawElbowGizmo(_LeftArm, _LeftHandTarget, true);
            if (_RightArm.IsValid)
                DrawElbowGizmo(_RightArm, _RightHandTarget, false);

            DrawKneeGizmo(_LeftLeg);
            DrawKneeGizmo(_RightLeg);
        }
        
        private void DrawTargetGizmo(TargetRecord target, Color color, Transform drivenBone)
        {
            Gizmos.color = color;

            if (drivenBone == null)
                return;

            // draw offset against the bone, as if the target were there
            var targetPos = target.GetInferredTargetPosition(drivenBone);
            var targetRot = target.GetInferredTargetRotation(drivenBone);

            Gizmos.DrawWireSphere(drivenBone.position, 0.02f);
            Gizmos.DrawLine(drivenBone.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.04f);
            Gizmos.DrawRay(targetPos, targetRot * Vector3.forward * 0.1f);
        }

        private void DrawElbowGizmo(ArmRecord arm, TargetRecord target, bool isLeft)
        {
            var handRotation = target.IsValid ? target.GetRotation() : arm.Hand.rotation;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(arm.Forearm.position, GetElbowHint(arm, handRotation, isLeft) * 0.25f);

            DrawHintGizmo(arm.Hint, arm.Forearm.position);
        }

        private void DrawKneeGizmo(LegRecord leg)
        {
            if (!leg.IsValid)
                return;
            
            if (Application.isPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(leg.Calf.position, leg.KneeHint * 0.25f);
            }

            DrawHintGizmo(leg.Hint, leg.Calf.position);
        }
        
        private static void DrawHintGizmo(Transform hint, Vector3 midJoint)
        {
            if (hint == null)
                return;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(hint.position, 0.03f);
            Gizmos.DrawLine(midJoint, hint.position);
        }
        
        [EnhancedFoldoutGroup("Bones")]
        [Button("Auto Detect Bones")]
        [ShowInFoldoutHeader]
        public void AutoDetectBones()
        {
            if (_Animator == null || !_Animator.isHuman)
            {
                Debug.LogError($"[{nameof(VRBodySolver)}] Auto detection requires a humanoid Animator", this);
                return;
            }

            _Hips = _Animator.GetBoneTransform(HumanBodyBones.Hips);
            _Spine = _Animator.GetBoneTransform(HumanBodyBones.Spine);
            _Chest = _Animator.GetBoneTransform(HumanBodyBones.Chest);
            _Neck = _Animator.GetBoneTransform(HumanBodyBones.Neck);
            _Head = _Animator.GetBoneTransform(HumanBodyBones.Head);

            _LeftArm.Shoulder = _Animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            _LeftArm.UpperArm = _Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _LeftArm.Forearm = _Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _LeftArm.Hand = _Animator.GetBoneTransform(HumanBodyBones.LeftHand);

            _RightArm.Shoulder = _Animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            _RightArm.UpperArm = _Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _RightArm.Forearm = _Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _RightArm.Hand = _Animator.GetBoneTransform(HumanBodyBones.RightHand);

            _LeftLeg.Thigh = _Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            _LeftLeg.Calf = _Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _LeftLeg.Foot = _Animator.GetBoneTransform(HumanBodyBones.LeftFoot);

            _RightLeg.Thigh = _Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _RightLeg.Calf = _Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            _RightLeg.Foot = _Animator.GetBoneTransform(HumanBodyBones.RightFoot);


            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        
        public enum FootGroundingMode
        {
            [Tooltip("Raycast feet to ground")] Calculate,
            [Tooltip("Read foot pose from the Grounding Anchor")] ReadAnchors,
            Disabled
        }
        
        [Serializable]
        public class TargetRecord
        {
            [SerializeField]
            private Transform _Target;

            [SerializeField]
            private Vector3 _PositionOffset;

            [SerializeField]
            private Vector3 _RotationOffset;

            public Transform Target
            {
                get => _Target;
                set => _Target = value;
            }

            public bool IsValid => _Target != null;

            public Vector3 GetPosition() => _Target.TransformPoint(_PositionOffset);
            public Quaternion GetRotation() => _Target.rotation * Quaternion.Euler(_RotationOffset);
            
            // inverse of the offset: where target sits to drive this bone. for gizmos
            public Quaternion GetInferredTargetRotation(Transform reference) => reference.rotation * Quaternion.Inverse(Quaternion.Euler(_RotationOffset));
            public Vector3 GetInferredTargetPosition(Transform reference) => reference.position - GetInferredTargetRotation(reference) * _PositionOffset;
        }

        [Serializable]
        public class ArmRecord
        {
            [SerializeField]
            [Tooltip("Optional clavicle bone.")]
            private Transform _Shoulder;

            [SerializeField]
            private Transform _UpperArm;

            [SerializeField]
            private Transform _Forearm;

            [SerializeField]
            private Transform _Hand;

            [SerializeField]
            [FormerlySerializedAs("_BendGoal")]
            [Tooltip("Optional elbow hint. Bends the elbow toward this transform.")]
            private Transform _Hint;

            [SerializeField]
            [Range(0f, 1f)]
            [FormerlySerializedAs("_BendGoalWeight")]
            private float _HintWeight = 1f;

            public Transform Shoulder
            {
                get => _Shoulder;
                set => _Shoulder = value;
            }

            public Transform UpperArm
            {
                get => _UpperArm;
                set => _UpperArm = value;
            }

            public Transform Forearm
            {
                get => _Forearm;
                set => _Forearm = value;
            }

            public Transform Hand
            {
                get => _Hand;
                set => _Hand = value;
            }

            public Transform Hint => _Hint;
            public float HintWeight => _HintWeight;

            public bool IsValid => _UpperArm != null && _Forearm != null && _Hand != null;
        }

        [Serializable]
        public class LegRecord
        {
            [SerializeField]
            private Transform _Thigh;

            [SerializeField]
            private Transform _Calf;

            [SerializeField]
            private Transform _Foot;

            [SerializeField]
            [FormerlySerializedAs("_BendGoal")]
            [Tooltip("Optional knee hint. Default bends forward.")]
            private Transform _Hint;

            [SerializeField]
            private Transform _GroundingAnchor;

            public Transform Thigh
            {
                get => _Thigh;
                set => _Thigh = value;
            }

            public Transform Calf
            {
                get => _Calf;
                set => _Calf = value;
            }

            public Transform Foot
            {
                get => _Foot;
                set => _Foot = value;
            }

            public bool IsValid => _Thigh != null && _Calf != null && _Foot != null;

            public Transform Hint => _Hint;
            public Transform GroundingAnchor { get => _GroundingAnchor; set => _GroundingAnchor = value; }
            public Vector3 FootPosition { get; private set; }
            public Quaternion FootRotation { get; private set; }
            public Vector3 KneeHint { get; set; }
            public float MaxLength { get; private set; }
            public float GroundOffset { get; private set; }

            private bool _grounded;
            private Quaternion _groundRotationOffset = Quaternion.identity;

            public void Record()
            {
                FootPosition = _Foot.position;
                FootRotation = _Foot.rotation;

                var upperLength = Vector3.Distance(_Thigh.position, _Calf.position);
                var lowerLength = Vector3.Distance(_Calf.position, _Foot.position);
                MaxLength = upperLength + lowerLength;
            }

            public void ApplyGrounding(float offsetTarget, float groundY, Vector3 groundNormal, float deltaTime, float footSpeed, float rotationSpeed, float maxAngle, float heightOffset)
            {
                if (_grounded)
                {
                    GroundOffset = offsetTarget;
                }
                else
                {
                    // ease down on first contact
                    GroundOffset = Mathf.Lerp(GroundOffset, offsetTarget, deltaTime * footSpeed);
                    if (Mathf.Abs(GroundOffset - offsetTarget) < 0.005f)
                    {
                        GroundOffset = offsetTarget;
                        _grounded = true;
                    }
                }
                FootPosition += Vector3.up * (GroundOffset + heightOffset);

                // don't sink below ground
                if (FootPosition.y < groundY)
                    FootPosition = new Vector3(FootPosition.x, groundY, FootPosition.z);

                var rotationTarget = maxAngle > 0f
                    ? Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, groundNormal), maxAngle)
                    : Quaternion.identity;
                _groundRotationOffset = Quaternion.Slerp(_groundRotationOffset, rotationTarget, deltaTime * rotationSpeed);
                FootRotation = _groundRotationOffset * FootRotation;
            }

            public void ClearGrounding()
            {
                _grounded = false;
                GroundOffset = 0f;
                _groundRotationOffset = Quaternion.identity;
            }
            
            public void WriteGroundingAnchor()
            {
                if (!IsValid || _GroundingAnchor == null)
                    return;
                _GroundingAnchor.SetPositionAndRotation(FootPosition, FootRotation);
            }
            
            public float ReadGroundingAnchors(float rootY, float heightOffset)
            {
                if (!IsValid || _GroundingAnchor == null)
                    return 0f;
                FootPosition = _GroundingAnchor.position;
                FootRotation = _GroundingAnchor.rotation;
                GroundOffset = FootPosition.y - heightOffset - rootY;
                return GroundOffset;
            }
        }
    }
}
