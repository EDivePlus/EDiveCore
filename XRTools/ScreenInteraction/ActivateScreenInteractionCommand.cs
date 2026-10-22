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
    // Activates the held interactable while pressed, like a VR trigger
    [Serializable]
    public class ActivateScreenInteractionCommand : AScreenInteractionCommand
    {
        [SerializeField]
        [Required]
        private XRBaseInteractable _Interactable;

        public override bool IsHold => true;

        public override UniTask Execute(ScreenInteractionCommandContext context, CancellationToken cancellationToken)
        {
            return context.Controller.Activate(_Interactable, cancellationToken);
        }
    }
}
