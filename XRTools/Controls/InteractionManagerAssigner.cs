// Author: František Holubec
// Created: 13.05.2025

using EDIVE.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.XRTools.Controls
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractionManagerAssigner : MonoBehaviour
    {
        private void Awake()
        {
            if (!TryGetComponent(out XRBaseInteractable interactable) || !AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                return;

            interactable.interactionManager = controlsManager.InteractionManager;
            if (interactable is BaseTeleportationInteractable teleportationInteractable)
            {
                teleportationInteractable.teleportationProvider = controlsManager.TeleportationProvider;
            }
        }
    }
}
