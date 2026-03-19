// Author: František Holubec
// Created: 13.05.2025

using EDIVE.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.XRTools.Controls
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(XRBaseInteractable))]
    public class InteractionManagerAssigner : MonoBehaviour
    {
        private void Awake()
        {
            if (!AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                return;

            var interactables = GetComponents<XRBaseInteractable>();
            foreach (var interactable in interactables)
            {
                interactable.interactionManager = controlsManager.InteractionManager;
                if (interactable is BaseTeleportationInteractable teleportationInteractable)
                {
                    teleportationInteractable.teleportationProvider = controlsManager.TeleportationProvider;
                }
            }
        }
    }
}
