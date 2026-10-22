// Author: Michal Petr
// Created: 22.10.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Commands the screen controls offer for this object
    [DisallowMultipleComponent]
    public class ScreenInteractable : MonoBehaviour
    {
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<AScreenInteractionCommand> _Commands = new();

        public IReadOnlyList<AScreenInteractionCommand> Commands => _Commands;

        private void Reset()
        {
            var interactable = GetComponentInChildren<XRGrabInteractable>();
            if (interactable == null)
                return;

            _Commands.Add(new GrabScreenInteractionCommand(interactable));
            _Commands.Add(new DropScreenInteractionCommand(interactable));
        }
    }
}
