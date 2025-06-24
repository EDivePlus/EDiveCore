// Author: Radim Holub
// Created: 18.06.2025

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EDIVE.XRTools.Interactions
{
    public abstract class AInteractorTransformer : MonoBehaviour
    {
        public abstract void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);
        public abstract bool IsManipulating();
    }
}
