// Author: Radim Holub
// Created: 08.04.2026

using EDIVE.NativeUtils;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace EDIVE.XRTools.Utils
{
    public class AutoCameraLookAt : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        [Tooltip("Optional camera transform variable. If not set, will use direct camera transform or Camera.main.")]
        private TransformScriptableVariable _CameraTransformVariable;

        [SerializeField]
        [Tooltip("Optional direct camera transform fallback.")]
        private Transform _CameraTransform;

        [SerializeField]
        [Tooltip("Object that will rotate. If not set, this transform will be used.")]
        private Transform _TargetTransform;

        [Header("Behaviour")]
        [SerializeField]
        private bool _ApplyOnAwake = true;

        [SerializeField]
        private bool _UpdateContinuously = true;

        [SerializeField]
        [Tooltip("If true, object forward will be inverted after look rotation.")]
        private bool _InvertForward = false;

        [SerializeField]
        [Tooltip("Keeps a stable up vector instead of using the camera up.")]
        private bool _UseWorldUp = true;

        [SerializeField]
        private Vector3 _WorldUp = Vector3.up;

        [Header("Smoothing")]
        [SerializeField]
        private bool _UseSmoothRotation = true;

        [SerializeField, Min(0.001f)]
        private float _RotationSmoothTime = 0.08f;

        private Vector3 _rotationVelocity;

        public Transform TargetTransform => _TargetTransform != null ? _TargetTransform : transform;

        public Transform CameraTransform =>
            _CameraTransformVariable != null && _CameraTransformVariable.Value != null
                ? _CameraTransformVariable.Value
                : _CameraTransform != null
                    ? _CameraTransform
                    : Camera.main != null
                        ? Camera.main.transform
                        : null;

        private void Awake()
        {
            if (_ApplyOnAwake)
                Refresh(true);
        }

        private void LateUpdate()
        {
            if (!_UpdateContinuously)
                return;

            Refresh(immediate: !_UseSmoothRotation);
        }

        public void SetCamera(Transform cameraTransform)
        {
            _CameraTransform = cameraTransform;
        }

        public void Refresh(bool immediate = false)
        {
            Transform target = TargetTransform;
            Transform cameraTransform = CameraTransform;

            if (target == null || cameraTransform == null)
                return;

            Vector3 direction = cameraTransform.position - target.position;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            if (_InvertForward)
                direction = -direction;

            Vector3 up = _UseWorldUp ? _WorldUp.normalized : cameraTransform.up;
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, up);

            if (immediate || !_UseSmoothRotation)
            {
                target.rotation = targetRotation;
                return;
            }

            target.rotation = RotationUtility.SmoothDampQuaternion(
                target.rotation,
                targetRotation,
                ref _rotationVelocity,
                _RotationSmoothTime);
        }

        [ContextMenu("Refresh Immediate")]
        private void RefreshImmediateContext()
        {
            Refresh(true);
        }
    }
}