// Author: Radim Holzb
// Created: 18.06.2025

using EDIVE.XRTools.Interactions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using XRScaleMode = UnityEngine.XR.Interaction.Toolkit.Interactors.ScaleMode; 

namespace EDIVE.XRTools.Controls
{
    [AddComponentMenu("XR/Transformers/Manipulation Interactor Transformer")]
    public class ManipulationInteractorTransformer : AInteractorTransformer
    {
        [Header("References")]
        [Tooltip("Reference na interactor.")]
        public XRBaseInteractor interactor;

        [Header("Manipulation Settings")]
        public bool manipulateAttachTransform = true;
        public float translateSpeed = 1f;
        public XRRayInteractor.RotateMode rotateMode = XRRayInteractor.RotateMode.RotateOverTime;
        public float rotateSpeed = 180f;
        public Transform rotateReferenceFrame;


        [Header("Input Readers")]
        public XRInputValueReader<Vector2> translateManipulationInput;
        public XRInputValueReader<Vector2> rotateManipulationInput;
        public XRInputValueReader<Vector2> directionalManipulationInput;

        [Header("Scale Settings")]
        public XRScaleMode scaleMode = XRScaleMode.ScaleOverTime;
        
        [Header("Scale Inputs")]
        public XRInputButtonReader scaleToggleInput;
        public XRInputValueReader<Vector2> scaleOverTimeInput;
        public XRInputValueReader<float> scaleDistanceDeltaInput;
        
        float _NearFarOffset = 0f;

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if (!manipulateAttachTransform || interactor == null || !interactor.hasSelection)
                return;

            var attachTransform = interactor.attachTransform;
            
            if (attachTransform != null)
            {
                attachTransform.position += transform.forward * _NearFarOffset;
            }

            // TRANSLATE ALONG LOCAL Z
            if (translateManipulationInput != null)
            {
                Vector2 translateInput = translateManipulationInput.ReadValue();
                //Debug.Log($"[ManipulationTransformer] Joystick input: {translateInput}");
                
                float translationAmount = translateInput.y * translateSpeed * Time.deltaTime;
                if (Mathf.Abs(translateInput.y) > 0.01f)
                {
                    _NearFarOffset += translationAmount;
                    Debug.Log($"[ManipulationTransformer] translationAmount: {translationAmount}");
                }
                else
                {
                    Debug.Log($"[ManipulationTransformer] Joystick input too small, not moving.");
                }
            }
            else
            {
                Debug.LogWarning("[ManipulationTransformer] translateManipulationInput is NULL!");
            }

            // ROTATE
            if (rotateMode == XRRayInteractor.RotateMode.RotateOverTime && rotateManipulationInput != null)
            {
                Vector2 rotateInput = rotateManipulationInput.ReadValue();
                float rotationAmount = rotateInput.x * rotateSpeed * Time.deltaTime;
                Vector3 upAxis = rotateReferenceFrame != null ? rotateReferenceFrame.up : attachTransform.up;
                attachTransform.Rotate(upAxis, rotationAmount, Space.World);
            }
            else if (rotateMode == XRRayInteractor.RotateMode.MatchDirection && directionalManipulationInput != null)
            {
                Vector2 directionalInput = directionalManipulationInput.ReadValue();
                if (directionalInput.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(directionalInput.x, directionalInput.y) * Mathf.Rad2Deg;
                    Vector3 upAxis = rotateReferenceFrame != null ? rotateReferenceFrame.up : attachTransform.up;
                    Quaternion rotation = Quaternion.AngleAxis(angle, upAxis);
                    attachTransform.rotation = rotation;
                }
            }
        }
        
        public override bool IsManipulating()
        {
            if (!manipulateAttachTransform || interactor == null || !interactor.hasSelection)
                return false;

            if (translateManipulationInput != null && translateManipulationInput.TryReadValue(out var translate) && translate.sqrMagnitude > 0.001f)
                return true;

            if (rotateManipulationInput != null && rotateManipulationInput.TryReadValue(out var rotate) && rotate.sqrMagnitude > 0.001f)
                return true;

            if (directionalManipulationInput != null && directionalManipulationInput.TryReadValue(out var directional) && directional.sqrMagnitude > 0.001f)
                return true;

            return false;
        }
    }
}
