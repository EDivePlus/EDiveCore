// Author: František Holubec
// Created: 18.09.2025


using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EDIVE.XRTools.Interactions
{
    [DisallowMultipleComponent]
    public class UIInteractionLayer : MonoBehaviour
    {
        [SerializeField]
        private InteractionLayerMask _InteractionLayers = 1;

        public InteractionLayerMask InteractionLayers
        {
            get => _InteractionLayers;
            set => _InteractionLayers = value;
        }
    }
}