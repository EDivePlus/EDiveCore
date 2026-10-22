// Author: Michal Petr
// Created: 22.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Picks up the targeted interactable, swaps with the held one
    [Serializable]
    public class GrabScreenInteractionCommand : AScreenInteractionCommand
    {
        [SerializeField]
        [Required]
        private XRGrabInteractable _Interactable;

        public override ScreenInteractionCommandSlot ShownWhen => ScreenInteractionCommandSlot.Targeted;

        public GrabScreenInteractionCommand() { }
        public GrabScreenInteractionCommand(XRGrabInteractable interactable)
        {
            _Interactable = interactable;
        }

        public override UniTask Execute(ScreenInteractionCommandContext context, CancellationToken cancellationToken)
        {
            context.Controller.Grab(_Interactable);
            return UniTask.CompletedTask;
        }
    }
}
