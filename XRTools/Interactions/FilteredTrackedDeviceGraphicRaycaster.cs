// Author: František Holubec
// Created: 18.09.2025

using System.Collections.Generic;
using EDIVE.External.DomainReloadHelper;
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

        [Tooltip("Poke keeps working this far behind the screen")]
        [SerializeField]
        private float _PokeBackDepth = 0.05f;

        [Tooltip("After a canvas closes mid press, pull back this far to press again")]
        [SerializeField]
        private float _PokeReleaseDistance = 0.01f;
        
        private readonly List<RaycastResult> _tempResults = new();
        private readonly HashSet<IUIInteractor> _pokeHovers = new();

        [ClearOnReload(NewInstance = true)]
        private static readonly HashSet<IUIInteractor> BLOCKED_POKES = new();

        public InteractionLayerMask InteractionLayers
        {
            get => _InteractionLayers;
            set => _InteractionLayers = value;
        }
        
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is not TrackedDeviceEventData trackedEventData || trackedEventData.interactor is not IXRInteractor xrInteractor)
                return;
            
            _tempResults.Clear();
            var interactor = trackedEventData.interactor;
            var isPoke = interactor.TryGetUIModel(out var model) && model.interactionType == UIInteractionType.Poke;
            var ignoreReversed = ignoreReversedGraphics;
            // Only a poke that started in front
            if (ignoreReversed && isPoke && _pokeHovers.Contains(interactor) && IsBehind(model.position))
                ignoreReversedGraphics = false;
            base.Raycast(eventData, _tempResults);
            ignoreReversedGraphics = ignoreReversed;

            if (isPoke && BLOCKED_POKES.Contains(interactor) && GetDepth(model.position) <= -_PokeReleaseDistance)
                BLOCKED_POKES.Remove(interactor);
            var blocked = isPoke && BLOCKED_POKES.Contains(interactor);

            var hit = false;
            foreach (var tempResult in _tempResults)
            {
                if (!CheckRaycastResult(tempResult, xrInteractor))
                    continue;

                if (!blocked)
                    resultAppendList.Add(tempResult);
                hit = true;
            }
            _tempResults.Clear();

            if (!isPoke)
                return;

            if (hit)
                _pokeHovers.Add(interactor);
            else
                _pokeHovers.Remove(interactor);
        }
        
        protected override void OnDisable()
        {
            foreach (var interactor in _pokeHovers)
            {
                if (IsPokeSelectingWithUI(interactor))
                    BLOCKED_POKES.Add(interactor);
            }
            _pokeHovers.Clear();
            base.OnDisable();
        }

        private bool IsBehind(Vector3 position)
        {
            var depth = GetDepth(position);
            return depth > 0 && depth <= _PokeBackDepth;
        }

        private float GetDepth(Vector3 position) => Vector3.Dot(position - transform.position, transform.forward);
        
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
