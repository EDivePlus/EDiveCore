// Author: František Holubec
// Created: 07.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.Kinematics
{
    [ExecuteAlways]
    public class TwoBoneHingeIK : MonoBehaviour
    {
        [SerializeField]
        private Transform _Root;

        [SerializeField]
        private Transform _Mid;

        [Tooltip("Turns only around hinge axis.")]
        [SerializeField]
        private Transform _Tip;

        [Tooltip("Optional. Child of tip, takes full target rotation.")]
        [SerializeField]
        private Transform _End;

        [Tooltip("Up = no tilt.")]
        [SerializeField]
        private Transform _Target;

        [Tooltip("Hinge axis = up.")]
        [SerializeField]
        private Transform _Axis;

        [SerializeField]
        private Quaternion _TipRotationOffset = Quaternion.identity;

        [ShowIf(nameof(_End))]
        [SerializeField]
        private Quaternion _EndRotationOffset = Quaternion.identity;

        [Tooltip("Max target tilt from hinge axis.")]
        [SerializeField]
        private float _MaxTilt = 20f;

        [Tooltip("Root and mid joint must stay in front of these.")]
        [SerializeField]
        [EnhancedTableList(ShowFoldout = false)]
        private PlaneLimit[] _JointLimits = Array.Empty<PlaneLimit>();

        [Tooltip("Mid joint can flip side only when bent less than this.")]
        [SerializeField]
        private float _FlipThreshold = 0.02f;

        private const float ANGLE_EPSILON = 0.01f;

        private float _upperLength;
        private float _lowerLength;
        private Vector3 _tipReference;
        private int _side = 1;

        public Transform Axis => _Axis;

        private bool IsReady => _Root && _Mid && _Tip && _Target && _Axis;

        private void OnEnable()
        {
            if (!IsReady)
                return;

            var axis = _Axis.up;
            _upperLength = Vector3.ProjectOnPlane(_Mid.position - _Root.position, axis).magnitude;
            _lowerLength = Vector3.ProjectOnPlane(_Tip.position - _Mid.position, axis).magnitude;
            _tipReference = Quaternion.Inverse(_Tip.rotation) * Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.forward)) < 0.9f ? Vector3.forward : Vector3.right).normalized;

            var line = Vector3.ProjectOnPlane(_Tip.position - _Root.position, axis);
            var bend = Vector3.Dot(_Mid.position - _Root.position, Vector3.Cross(axis, line));
            _side = bend >= 0f ? 1 : -1;

            if (Application.isPlaying)
                Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        private void LateUpdate()
        {
            Solve();
        }

        [BeforeRenderOrder(110)]
        private void OnBeforeRender()
        {
            Solve();
        }

        [Button]
        private void CaptureRotationOffsets()
        {
            _TipRotationOffset = Quaternion.Inverse(_Target.rotation) * _Tip.rotation;
            if (_End)
                _EndRotationOffset = Quaternion.Inverse(_Target.rotation) * _End.rotation;
        }

        public bool IsValid(Transform mover, Pose pose)
        {
            if (!IsReady)
                return true;

            var moverToPose = Matrix4x4.TRS(pose.position, pose.rotation.normalized, mover.lossyScale) * mover.worldToLocalMatrix;
            var targetMoves = _Target.IsChildOf(mover);
            var target = targetMoves ? moverToPose.MultiplyPoint3x4(_Target.position) : _Target.position;
            var targetUp = targetMoves ? moverToPose.MultiplyVector(_Target.up) : _Target.up;

            if (Vector3.Angle(targetUp, _Axis.up) > _MaxTilt + 0.01f)
                return false;

            var reach = Vector3.ProjectOnPlane(target - _Root.position, _Axis.up).magnitude;
            if (reach > _upperLength + _lowerLength + 0.001f || reach < Mathf.Abs(_upperLength - _lowerLength) - 0.001f)
                return false;

            if (!IsInFront(_Root.position, mover, moverToPose))
                return false;

            var elbow = SolveElbow(target, _side, out var bend);
            if (IsInFront(elbow, mover, moverToPose))
                return true;

            return bend < _FlipThreshold && IsInFront(SolveElbow(target, -_side, out _), mover, moverToPose);
        }

        private void Solve()
        {
            if (!IsReady || _upperLength <= 0f || _lowerLength <= 0f)
                return;

            var target = _Target.position;
            var elbow = SolveElbow(target, _side, out var bend);
            if (bend < _FlipThreshold && !IsInFront(elbow, null, Matrix4x4.identity))
            {
                var flipped = SolveElbow(target, -_side, out _);
                if (IsInFront(flipped, null, Matrix4x4.identity))
                {
                    _side = -_side;
                    elbow = flipped;
                }
            }

            var axis = _Axis.up;
            var tip = elbow + Vector3.ProjectOnPlane(target - elbow, axis).normalized * _lowerLength;
            var tipRotation = _Target.rotation * _TipRotationOffset;

            RotateToward(_Root, _Mid.position, elbow, axis);
            RotateToward(_Mid, _Tip.position, tip, axis);
            RotateToward(_Tip, _Tip.position + _Tip.rotation * _tipReference, _Tip.position + tipRotation * _tipReference, axis);

            if (_End)
                SetRotation(_End, _Target.rotation * _EndRotationOffset);
        }

        private Vector3 SolveElbow(Vector3 target, int side, out float bend)
        {
            var axis = _Axis.up;
            var root = _Root.position;
            var offset = Vector3.ProjectOnPlane(target - root, axis);
            var minReach = Mathf.Abs(_upperLength - _lowerLength) + 0.0001f;
            var maxReach = _upperLength + _lowerLength - 0.0001f;
            var distance = Mathf.Clamp(offset.magnitude, minReach, maxReach);
            var direction = offset.sqrMagnitude > 0.00000001f ? offset.normalized : Vector3.ProjectOnPlane(_Root.forward, axis).normalized;

            var along = (_upperLength * _upperLength - _lowerLength * _lowerLength + distance * distance) / (2f * distance);
            bend = Mathf.Sqrt(Mathf.Max(_upperLength * _upperLength - along * along, 0f));
            var height = Vector3.Dot(_Mid.position - root, axis);
            return root + direction * along + Vector3.Cross(axis, direction) * (bend * side) + axis * height;
        }

        private bool IsInFront(Vector3 point, Transform mover, Matrix4x4 moverToPose)
        {
            foreach (var limit in _JointLimits)
            {
                if (!limit.IsInFront(point, mover, moverToPose))
                    return false;
            }

            return true;
        }

        private static void RotateToward(Transform joint, Vector3 current, Vector3 desired, Vector3 axis)
        {
            var from = Vector3.ProjectOnPlane(current - joint.position, axis);
            var to = Vector3.ProjectOnPlane(desired - joint.position, axis);
            if (from.sqrMagnitude < 0.00000001f || to.sqrMagnitude < 0.00000001f)
                return;

            var angle = Vector3.SignedAngle(from, to, axis);
            if (Mathf.Abs(angle) > ANGLE_EPSILON)
                joint.rotation = Quaternion.AngleAxis(angle, axis) * joint.rotation;
        }

        private static void SetRotation(Transform joint, Quaternion rotation)
        {
            if (Quaternion.Angle(joint.rotation, rotation) > ANGLE_EPSILON)
                joint.rotation = rotation;
        }
    }
}
