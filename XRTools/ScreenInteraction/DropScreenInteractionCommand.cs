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
    // Lets go of the held interactable
    [Serializable]
    public class DropScreenInteractionCommand : AScreenInteractionCommand
    {
        [SerializeField]
        [Required]
        private XRGrabInteractable _Interactable;

        public DropScreenInteractionCommand() { }
        public DropScreenInteractionCommand(XRGrabInteractable interactable)
        {
            _Interactable = interactable;
        }

        public override UniTask Execute(ScreenInteractionCommandContext context, CancellationToken cancellationToken)
        {
            context.Controller.Drop(_Interactable);
            return UniTask.CompletedTask;
        }
    }
}
