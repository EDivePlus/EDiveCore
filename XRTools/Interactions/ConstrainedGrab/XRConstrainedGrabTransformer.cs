// Author: František Holubec
// Created: 07.10.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace EDIVE.XRTools.Interactions.ConstrainedGrab
{
    public class XRConstrainedGrabTransformer : XRBaseGrabTransformer
    {
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<APoseConstraint> _Constraints = new();

        [Tooltip("Slide axis = up. Blocked moves try turning around it.")]
        [SerializeField]
        private Transform _SlideAxis;

        [SerializeField]
        private float _StepDistance = 0.01f;

        [SerializeField]
        private float _StepAngle = 2f;

        private const int MAX_STEPS = 96;
        private static readonly float[] SLIDE_ANGLES = {0f, 30f, -30f, 60f, -60f, 90f, -90f};

        private readonly Quaternion[] _rotationSteps = new Quaternion[3];
        private Pose _lastValidPose;
        private bool _isFree;

        protected override RegistrationMode registrationMode => RegistrationMode.SingleAndMultiple;

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);
            _lastValidPose = grabInteractable.transform.GetWorldPose();
            _isFree = !IsValid(grabInteractable.transform, _lastValidPose);
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            switch (updatePhase)
            {
                case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                {
                    var mover = grabInteractable.transform;
                    if (_isFree)
                    {
                        _lastValidPose = targetPose;
                        _isFree = !IsValid(mover, targetPose);
                        break;
                    }

                    targetPose = Move(mover, targetPose);
                    _lastValidPose = targetPose;
                    break;
                }
            }
        }

        private bool IsValid(Transform mover, Pose pose)
        {
            foreach (var constraint in _Constraints)
            {
                if (constraint != null && !constraint.IsValid(mover, pose))
                    return false;
            }

            return true;
        }

        private Pose Move(Transform mover, Pose target)
        {
            var axis = _SlideAxis ? _SlideAxis.up : Vector3.up;
            var pose = _lastValidPose;
            for (var i = 0; i < MAX_STEPS; i++)
            {
                var cost = Cost(pose, target);
                if (cost < 0.01f || !TryStep(mover, ref pose, target, axis, cost))
                    break;
            }

            return pose;
        }

        private bool TryStep(Transform mover, ref Pose pose, Pose target, Vector3 axis, float cost)
        {
            var toTarget = target.position - pose.position;
            var distance = toTarget.magnitude;
            var positionStep = distance > 0.0001f ? toTarget * Mathf.Min(1f, _StepDistance / distance) : Vector3.zero;

            var delta = target.rotation * Quaternion.Inverse(pose.rotation);
            SplitTwist(delta, axis, out var swing, out var twist);
            _rotationSteps[0] = delta;
            _rotationSteps[1] = twist;
            _rotationSteps[2] = swing;

            if (TryCandidate(mover, ref pose, target, cost, new Pose(pose.position + positionStep, (LimitStep(delta) * pose.rotation).normalized)))
                return true;

            foreach (var angle in SLIDE_ANGLES)
            {
                if (TryCandidate(mover, ref pose, target, cost, new Pose(pose.position + Quaternion.AngleAxis(angle, axis) * positionStep, pose.rotation)))
                    return true;
            }

            foreach (var step in _rotationSteps)
            {
                if (TryCandidate(mover, ref pose, target, cost, new Pose(pose.position, (LimitStep(step) * pose.rotation).normalized)))
                    return true;
            }

            return false;
        }

        private bool TryCandidate(Transform mover, ref Pose pose, Pose target, float cost, Pose candidate)
        {
            if (Cost(candidate, target) >= cost - 0.001f || !IsValid(mover, candidate))
                return false;

            pose = candidate;
            return true;
        }

        private float Cost(Pose pose, Pose target)
        {
            return Vector3.Distance(pose.position, target.position) / _StepDistance + Quaternion.Angle(pose.rotation, target.rotation) / _StepAngle;
        }

        private Quaternion LimitStep(Quaternion rotation)
        {
            var angle = Quaternion.Angle(Quaternion.identity, rotation);
            return angle < 0.01f ? Quaternion.identity : Quaternion.Slerp(Quaternion.identity, rotation, Mathf.Min(1f, _StepAngle / angle));
        }

        private static void SplitTwist(Quaternion rotation, Vector3 axis, out Quaternion swing, out Quaternion twist)
        {
            var projection = Vector3.Dot(new Vector3(rotation.x, rotation.y, rotation.z), axis) * axis;
            twist = new Quaternion(projection.x, projection.y, projection.z, rotation.w);
            var magnitude = Mathf.Sqrt(twist.x * twist.x + twist.y * twist.y + twist.z * twist.z + twist.w * twist.w);
            twist = magnitude > 0.000001f ? new Quaternion(twist.x / magnitude, twist.y / magnitude, twist.z / magnitude, twist.w / magnitude) : Quaternion.identity;
            swing = rotation * Quaternion.Inverse(twist);
        }
    }
}
