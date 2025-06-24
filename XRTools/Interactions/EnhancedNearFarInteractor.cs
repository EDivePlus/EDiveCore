// Author: Radim Holub
// Created: 23.06.2025
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


namespace EDIVE.XRTools.Interactions
{
    public class EnhancedNearFarInteractor : NearFarInteractor
    {
        [SerializeField]
        List<AInteractorTransformer> _Transformers = new();

        public bool IsManipulatingAttachTransform()
        {
            foreach (var transformer in _Transformers)
            {
                if (transformer != null && transformer.IsManipulating())
                    return true;
            }
            return false;
        }
        
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            foreach (var transformer in _Transformers)
            {
                if (transformer != null)
                    transformer.PreprocessInteractor(updatePhase);
            }
        }
        
        
    }
}
