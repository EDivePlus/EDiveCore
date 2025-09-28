// Author: František Holubec
// Created: 18.09.2025

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace EDIVE.XRTools.Interactions
{
    public class FilteredTrackedDeviceGraphicRaycaster : TrackedDeviceGraphicRaycaster
    {
        [SerializeField]
        private InteractionLayerMask _InteractionLayers = 1;
        
        public InteractionLayerMask InteractionLayers
        {
            get => _InteractionLayers;
            set => _InteractionLayers = value;
        }
        
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is not TrackedDeviceEventData trackedEventData || trackedEventData.interactor is not IXRInteractor xrInteractor)
                return;
            
            var tempResults = new List<RaycastResult>();
            base.Raycast(eventData, tempResults);

            foreach (var tempResult in tempResults)
            {
                if (CheckRaycastResult(tempResult, xrInteractor)) 
                    resultAppendList.Add(tempResult);
            }
        }
        
        private bool CheckRaycastResult(RaycastResult raycastResult, IXRInteractor xrInteractor)
        {
            if (TryFindLayerMask(raycastResult.gameObject.transform, out var layerMask))
            {
                return (layerMask.InteractionLayers & xrInteractor.interactionLayers) != 0;
            }
            
            return (_InteractionLayers & xrInteractor.interactionLayers) != 0;
        }
        
        private bool TryFindLayerMask(Transform target, out UIInteractionLayer layerMask)
        {
            layerMask = null;
            while (target != null && target != transform)
            {
                if (target.TryGetComponent(out layerMask))
                    return true;

                target = target.parent;
            }
            return false;
        }
    }
}
