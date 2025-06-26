// Author: František Holubec
// Created: 26.06.2025

using EDIVE.Core.Services;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace EDIVE.XRTools.Tablet
{
    public class TabletManager : AServiceBehaviour<TabletManager>
    {
        [SerializeField]
        private TabletController _Tablet;

        [SerializeField]
        private TransformScriptableVariable _CameraTransformVariable;

        [SerializeField]
        private InputActionReference _RepositionAction;

        [SerializeField]
        private Vector3 _PositionOffset;

        private Transform CameraTransform => _CameraTransformVariable != null && _CameraTransformVariable.Value != null
                ? _CameraTransformVariable.Value
                : Camera.main?.transform;

        protected override void Awake()
        {
            base.Awake();
            if(_RepositionAction)
                _RepositionAction.action.performed += OnRepositionPerformed;
        }

        private void OnRepositionPerformed(InputAction.CallbackContext context)
        {
            RepositionTablet();
        }

        [Button]
        public void RepositionTablet()
        {
            var target = CameraTransform;
            if (target == null)
                return;

            var position = target.position +
                           target.right * _PositionOffset.x +
                           target.forward * _PositionOffset.z +
                           Vector3.up * _PositionOffset.y;
            _Tablet.transform.position = position;
            _Tablet.transform.localScale = Vector3.one;
            FaceKeyboardAtTarget(target);
        }

        private void FaceKeyboardAtTarget(Transform target)
        {
            var forward = (_Tablet.transform.position - target.position).normalized;
            BurstMathUtility.OrthogonalLookRotation(forward, Vector3.up, out var newTarget);
            _Tablet.transform.rotation = newTarget;
        }
    }
}
