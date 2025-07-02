// Author: František Holubec
// Created: 08.06.2025

using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace EDIVE.XRTools.Interactions
{
    public class XRRotationGrabTransformer : XRBaseGrabTransformer
    {
        [SerializeField]
        private bool _ApplyTwist = true;

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            switch (updatePhase)
            {
                case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                {
                    UpdateTarget(grabInteractable, ref targetPose);
                    break;
                }
            }
        }

        private void UpdateTarget(XRGrabInteractable grabInteractable, ref Pose targetPose)
        {
            var interactor = grabInteractable.interactorsSelecting[0];
            var interactorAttachTransform = interactor.GetAttachTransform(grabInteractable);
            var interactorAttachPose = interactorAttachTransform.GetWorldPose();

            var objectTransform = grabInteractable.transform;
            var thisTransformPose = objectTransform.GetWorldPose();
            var thisAttachTransform = grabInteractable.GetAttachTransform(interactor);

            var objectAttachPos = thisAttachTransform.position;
            var initialDir = objectAttachPos - thisTransformPose.position;
            var targetDir = interactorAttachPose.position - thisTransformPose.position;

            if (initialDir.sqrMagnitude < 0.0001f || targetDir.sqrMagnitude < 0.0001f)
                return;

            var rotationDelta = Quaternion.FromToRotation(initialDir, targetDir);
            var newRotation = rotationDelta * thisTransformPose.rotation;

            if (_ApplyTwist)
            {
                var axis = targetDir.normalized;
                var objectUp = rotationDelta * thisAttachTransform.up;
                var controllerUp = interactorAttachTransform.up;

                var twist = Quaternion.FromToRotation(ProjectOnPlane(objectUp, axis), ProjectOnPlane(controllerUp, axis));
                newRotation = twist * newRotation;
            }

            targetPose.rotation = newRotation;
        }

        private static Vector3 ProjectOnPlane(Vector3 vector, Vector3 normal)
        {
            return vector - Vector3.Dot(vector, normal) * normal;
        }
    }
}
