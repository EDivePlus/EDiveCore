// Author: František Holubec
// Created: 16.06.2026

using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.Utils
{
    public class PlaceAtCast : MonoBehaviour
    {
        public enum DirectionSpace
        {
            Self,
            World
        }

        public enum Axis
        {
            Up,
            Down,
            Forward,
            Back,
            Right,
            Left
        }

        [Header("References")]
        [SerializeField]
        [Tooltip("Transform that will be moved to the hit point. If not set, this transform is used.")]
        private Transform _TargetTransform;

        [Header("Ray")]
        [SerializeField]
        [Tooltip("Direction of the ray. Interpreted in the space selected below.")]
        private Vector3 _Direction = Vector3.down;

        [SerializeField]
        [Tooltip("Self: direction is relative to this transform's rotation. World: direction is in world space.")]
        private DirectionSpace _Space = DirectionSpace.World;

        [SerializeField, Min(0f)]
        [Tooltip("Maximum cast distance. The target is placed here when nothing is hit.")]
        private float _MaxDistance = 10f;

        [SerializeField]
        private LayerMask _LayerMask = ~0;

        [SerializeField]
        [Tooltip("Whether the cast should hit trigger colliders.")]
        private QueryTriggerInteraction _TriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Alignment")]
        [SerializeField]
        [Tooltip("Local axis of the target that will be aligned to the hit surface normal.")]
        private Axis _AlignAxis = Axis.Up;

        [SerializeField]
        [Tooltip("When nothing is hit, also align the target to this transform's orientation at max distance.")]
        private bool _MatchOrientationOnMiss = true;

        [Header("Smoothing")]
        [SerializeField]
        private bool _UseSmoothing;

        [SerializeField, Min(0.001f)]
        [HideIf(nameof(_UseSmoothing), false)]
        private float _PositionSmoothTime = 0.08f;

        [SerializeField, Min(0.001f)]
        [HideIf(nameof(_UseSmoothing), false)]
        private float _RotationSmoothTime = 0.08f;

        public Transform TargetTransform => _TargetTransform != null ? _TargetTransform : transform;

        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;

        private void Update()
        {
            Refresh(immediate: !_UseSmoothing);
        }

        [Button]
        public void Refresh(bool immediate = false)
        {
            var target = TargetTransform;
            if (target == null)
                return;

            var origin = transform.position;
            var direction = GetWorldDirection();
            if (direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();

            Vector3 targetPosition;
            Quaternion targetRotation;

            if (Physics.Raycast(origin, direction, out var hit, _MaxDistance, _LayerMask, _TriggerInteraction))
            {
                targetPosition = hit.point;
                targetRotation = AlignToNormal(transform.rotation, hit.normal);
            }
            else
            {
                targetPosition = origin + direction * _MaxDistance;
                targetRotation = _MatchOrientationOnMiss ? transform.rotation : target.rotation;
            }

            if (immediate || !_UseSmoothing)
            {
                target.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            target.position = Vector3.SmoothDamp(target.position, targetPosition, ref _positionVelocity, _PositionSmoothTime);
            target.rotation = RotationUtility.SmoothDampQuaternion(target.rotation, targetRotation, ref _rotationVelocity, _RotationSmoothTime);
        }

        private Vector3 GetWorldDirection()
        {
            return _Space == DirectionSpace.Self ? transform.TransformDirection(_Direction) : _Direction;
        }
        
        private Quaternion AlignToNormal(Quaternion baseRotation, Vector3 normal)
        {
            if (normal.sqrMagnitude < 0.0001f)
                return baseRotation;

            var currentAxis = baseRotation * GetAxisVector(_AlignAxis);
            return Quaternion.FromToRotation(currentAxis, normal.normalized) * baseRotation;
        }

        private static Vector3 GetAxisVector(Axis axis)
        {
            return axis switch
            {
                Axis.Up => Vector3.up,
                Axis.Down => Vector3.down,
                Axis.Forward => Vector3.forward,
                Axis.Back => Vector3.back,
                Axis.Right => Vector3.right,
                Axis.Left => Vector3.left,
                _ => Vector3.up
            };
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position;
            var direction = GetWorldDirection();
            if (direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            var hasHit = Physics.Raycast(origin, direction, out var hit, _MaxDistance, _LayerMask, _TriggerInteraction);
            var end = hasHit ? hit.point : origin + direction * _MaxDistance;

            Gizmos.color = hasHit ? Color.green : Color.red;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, 0.05f);

            if (hasHit)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.25f);
            }
        }
    }
}
