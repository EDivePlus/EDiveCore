// Author: František Holubec
// Created: 27.10.2026

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.Interactions
{
    // No hover from a hand while its poke is at the screen
    public class UIPokeHoverFilter : MonoBehaviour, IXRHoverFilter
    {
        [SerializeField]
        private RectTransform _Screen;

        [Tooltip("Zone in front of the screen")]
        [SerializeField]
        private float _FrontDistance = 0.1f;

        [Tooltip("Zone behind the screen")]
        [SerializeField]
        private float _BackDistance = 0.05f;

        private readonly List<IXRGroupMember> _groupMembers = new();

        public bool canProcess => isActiveAndEnabled && _Screen;

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            if (interactor is XRPokeInteractor || interactor is not IXRGroupMember {containingGroup: { } group})
                return true;

            _groupMembers.Clear();
            group.GetGroupMembers(_groupMembers);
            foreach (var member in _groupMembers)
            {
                if (member is XRPokeInteractor poke && poke.isActiveAndEnabled && IsAtScreen(poke.GetAttachTransform(null).position))
                    return false;
            }
            return true;
        }

        private bool IsAtScreen(Vector3 position)
        {
            var depth = Vector3.Dot(position - _Screen.position, -_Screen.forward);
            if (depth > _FrontDistance || depth < -_BackDistance)
                return false;

            return _Screen.rect.Contains(_Screen.InverseTransformPoint(position));
        }
    }
}
